using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>
/// Firma del callback (D-121, contrato §3): X-Bridge-Signature = t={unix},v1={HMAC-SHA256(secreto,
/// t + "." + cuerpo)}. Se rechaza una firma inválida o con más de 5 minutos de antigüedad.
/// </summary>
public static class BridgeSignature
{
    public const string Cabecera = "X-Bridge-Signature";
    public static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(5);

    public static string Firmar(string cuerpo, string secreto, DateTimeOffset cuando)
    {
        var t = cuando.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return $"t={t},v1={Hmac(secreto, t, cuerpo)}";
    }

    public static bool Verificar(string? cabecera, string cuerpo, string secreto, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(cabecera) || string.IsNullOrEmpty(secreto)) return false;
        var partes = cabecera.Split(',')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.Ordinal);
        if (!partes.TryGetValue("t", out var t) || !partes.TryGetValue("v1", out var v1)) return false;
        if (!long.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var unix)) return false;
        if ((ahora - DateTimeOffset.FromUnixTimeSeconds(unix)).Duration() > Tolerancia) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hmac(secreto, t, cuerpo)),
            Encoding.ASCII.GetBytes(v1.ToLowerInvariant()));
    }

    private static string Hmac(string secreto, string t, string cuerpo) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(t + "." + cuerpo)));
}

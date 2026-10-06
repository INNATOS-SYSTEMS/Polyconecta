using System.Text.Json;
using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Application.Plataforma.Erp;

/// <summary>Resultado de una transacción del bridge, del callback o de la consulta (contrato §3).</summary>
public sealed record ResultadoBridge(
    string IdempotencyKey,
    string Status,
    string? Folio,
    string? IdErp,
    string? DocumentosJson,
    string? ErrorCode,
    string? ErrorMessage);

public enum EfectoCallback
{
    Aplicado,
    /// <summary>Repetido, tardío o todavía no terminal: no cambia nada (§3, "Orden").</summary>
    Ignorado,
    NoEncontrado,
}

/// <summary>
/// Aplica el resultado del bridge: guarda folio e id ERP en el documento y lo deja Confirmado, o
/// guarda el error y lo deja en Error (CT-13, CT-15). Un callback repetido o desordenado no cambia
/// un documento ya confirmado.
/// </summary>
public sealed class ConfirmarSincronizacion(IOutboxStore outbox, IDocumentosSincronizables documentos, IClock clock)
    : IUseCase<ResultadoBridge, EfectoCallback>
{
    public async Task<EfectoCallback> ExecuteAsync(ResultadoBridge request, CancellationToken cancellationToken = default)
    {
        var mensaje = await outbox.PorLlaveAsync(request.IdempotencyKey, cancellationToken);
        if (mensaje is null) return EfectoCallback.NoEncontrado;
        if (mensaje.Status == OutboxStatus.Confirmado) return EfectoCallback.Ignorado;

        var documento = await documentos.BuscarAsync(mensaje.DocumentType, mensaje.DocumentId, cancellationToken);
        var ahora = clock.Now;
        switch (request.Status)
        {
            case "CONFIRMED":
                mensaje.Confirmar(ahora);
                documento?.Sync.Confirmar(request.Folio, request.IdErp, request.DocumentosJson, ahora);
                return EfectoCallback.Aplicado;
            case "FAILED" or "DEAD_LETTER":
                var codigo = request.ErrorCode ?? "SDK_ERROR";
                var texto = request.ErrorMessage ?? request.Status;
                mensaje.MarcarError(codigo, texto, ahora);
                documento?.Sync.MarcarError(codigo, texto, ahora);
                return EfectoCallback.Aplicado;
            default:
                return EfectoCallback.Ignorado;
        }
    }

    /// <summary>Lee el cuerpo del callback o de GET /transactions/{id}.</summary>
    public static ResultadoBridge Leer(JsonElement cuerpo)
    {
        string? Texto(JsonElement e, string nombre) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nombre, out var v) && v.ValueKind != JsonValueKind.Null
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText())
                : null;

        var result = cuerpo.TryGetProperty("result", out var r) ? r : default;
        var error = cuerpo.TryGetProperty("error", out var e) ? e : default;
        string? folio = Texto(result, "folio");
        string? idErp = Texto(result, "id_erp");
        string? documentosJson = null;
        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("documentos", out var docs) && docs.ValueKind == JsonValueKind.Array)
        {
            documentosJson = docs.GetRawText();
            // Con varios documentos (TRASPASO, CIERRE_PRODUCCION), folio e id son los del primero.
            if (docs.GetArrayLength() > 0)
            {
                folio ??= Texto(docs[0], "folio");
                idErp ??= Texto(docs[0], "id_erp");
            }
        }
        return new ResultadoBridge(
            cuerpo.GetProperty("idempotency_key").GetString()!,
            cuerpo.GetProperty("status").GetString()!,
            folio, idErp, documentosJson, Texto(error, "code"), Texto(error, "message"));
    }
}

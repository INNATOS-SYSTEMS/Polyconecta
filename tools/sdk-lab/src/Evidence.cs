using System.Text.Json;

namespace SdkLab;

/// <summary>Cada invocación deja una línea en evidence/log-YYYYMMDD.jsonl: es la evidencia para el registro de resultados de la matriz.</summary>
internal static class Evidence
{
    public static void Append(LabConfig? cfg, string command, string[] args, object result)
    {
        try
        {
            var dir = cfg?.ResolveEvidenceDir() ?? Path.Combine(AppContext.BaseDirectory, "evidence");
            Directory.CreateDirectory(dir);
            var line = JsonSerializer.Serialize(new { ts = DateTimeOffset.Now, command, args, result }, Json.Options);
            File.AppendAllText(Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.jsonl"), line + Environment.NewLine);
        }
        catch { /* la evidencia nunca debe romper el comando */ }
    }
}

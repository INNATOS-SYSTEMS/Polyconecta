using System.Text.Json;

namespace SdkLab;

public sealed class LabConfig
{
    public string SdkPath { get; set; } = @"C:\Program Files (x86)\Compac\COMERCIAL";
    public string LabCompanyPath { get; set; } = "";
    public string LabDatabase { get; set; } = "";
    public string SqlServer { get; set; } = "";
    public string SqlUser { get; set; } = "sdklab_ro";
    /// <summary>Nombre de la variable de entorno con la contraseña. Nunca la contraseña misma.</summary>
    public string SqlPasswordEnv { get; set; } = "SDKLAB_SQL_PASSWORD";
    public string[] ForbiddenDatabases { get; set; } = [];
    public string EvidenceDir { get; set; } = "evidence";

    public static LabConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "lab.config.json");
        if (!File.Exists(path))
            throw new LabException("CONFIG_MISSING", $"No existe {path}. Copia lab.config.example.json y complétalo.");
        var cfg = JsonSerializer.Deserialize<LabConfig>(File.ReadAllText(path), Json.Options)
                  ?? throw new LabException("CONFIG_INVALID", "lab.config.json vacío.");
        if (string.IsNullOrWhiteSpace(cfg.LabCompanyPath) || string.IsNullOrWhiteSpace(cfg.LabDatabase) || string.IsNullOrWhiteSpace(cfg.SqlServer))
            throw new LabException("CONFIG_INVALID", "labCompanyPath, labDatabase y sqlServer son obligatorios.");
        return cfg;
    }

    public string ResolveEvidenceDir() =>
        Path.IsPathRooted(EvidenceDir) ? EvidenceDir : Path.Combine(AppContext.BaseDirectory, EvidenceDir);
}

public sealed class LabException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

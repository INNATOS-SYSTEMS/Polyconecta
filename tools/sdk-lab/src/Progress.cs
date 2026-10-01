namespace SdkLab;

/// <summary>
/// Bitácora en vivo (S-01, S-02): cada llamada al SDK se escribe a disco ANTES y DESPUÉS de ejecutarse, con hora y PID.
/// Si el proceso se cuelga, la última línea "→" dice en qué llamada.
/// </summary>
internal static class Progress
{
    private static string? _path;

    public static void Init(LabConfig cfg)
    {
        var dir = cfg.ResolveEvidenceDir();
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "progress.log");
    }

    public static void Log(string message)
    {
        if (_path is null) return;
        try { File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} pid={Environment.ProcessId} {message}{Environment.NewLine}"); }
        catch (IOException) { /* la bitácora nunca debe tumbar el experimento */ }
    }

    public static int Call(string fn, Func<int> call)
    {
        Log($"→ {fn}");
        var rc = call();
        Log($"← {fn} rc={rc}");
        return rc;
    }

    public static void Call(string fn, Action call)
    {
        Log($"→ {fn}");
        call();
        Log($"← {fn}");
    }
}

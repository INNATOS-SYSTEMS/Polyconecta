using Microsoft.Data.SqlClient;

namespace SdkLab;

/// <summary>
/// Barrera de seguridad: ningún comando toca CONTPAQi ni SQL si la base no es
/// inequívocamente el laboratorio. Las pruebas dejan documentos afectados no reversibles.
/// </summary>
public static class Guard
{
    public const string MarkerProperty = "POLYCONECTA_LAB";

    public sealed record Report(bool Ok, List<string> Failures, Dictionary<string, object?> Facts);

    public static SqlConnection OpenSql(LabConfig cfg)
    {
        var password = Environment.GetEnvironmentVariable(cfg.SqlPasswordEnv);
        if (string.IsNullOrEmpty(password))
            throw new LabException("SQL_PASSWORD_MISSING", $"Falta la variable de entorno {cfg.SqlPasswordEnv}.");
        var cs = new SqlConnectionStringBuilder
        {
            DataSource = cfg.SqlServer,
            InitialCatalog = cfg.LabDatabase,
            UserID = cfg.SqlUser,
            Password = password,
            TrustServerCertificate = true,
            ApplicationName = "sdklab",
            ConnectTimeout = 10,
        };
        var conn = new SqlConnection(cs.ConnectionString);
        conn.Open();
        return conn;
    }

    public static Report Evaluate(LabConfig cfg)
    {
        var failures = new List<string>();
        var facts = new Dictionary<string, object?>();

        if (cfg.ForbiddenDatabases.Any(d => d.Equals(cfg.LabDatabase, StringComparison.OrdinalIgnoreCase)))
            failures.Add($"labDatabase '{cfg.LabDatabase}' está en forbiddenDatabases.");

        var companyDir = Path.GetFileName(cfg.LabCompanyPath.TrimEnd('\\', '/'));
        facts["companyDir"] = companyDir;
        if (!companyDir.Equals(cfg.LabDatabase, StringComparison.OrdinalIgnoreCase))
            failures.Add($"labCompanyPath termina en '{companyDir}' pero labDatabase es '{cfg.LabDatabase}'. Deben coincidir.");

        try
        {
            using var conn = OpenSql(cfg);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DB_NAME()";
            var db = (string?)cmd.ExecuteScalar();
            facts["dbName"] = db;
            if (!string.Equals(db, cfg.LabDatabase, StringComparison.OrdinalIgnoreCase))
                failures.Add($"DB_NAME() devolvió '{db}', se esperaba '{cfg.LabDatabase}'.");

            cmd.CommandText = "SELECT CAST(value AS nvarchar(20)) FROM sys.extended_properties WHERE class = 0 AND name = @n";
            cmd.Parameters.AddWithValue("@n", MarkerProperty);
            var marker = (string?)cmd.ExecuteScalar();
            facts["marker"] = marker;
            if (marker != "1")
                failures.Add($"La base no tiene la propiedad extendida {MarkerProperty}=1. No se puede confirmar que sea una copia de laboratorio (ver Install-Workstation.ps1 / Restore-Lab.ps1).");

            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT IS_MEMBER('db_owner')";
            var isOwner = cmd.ExecuteScalar();
            facts["sqlLoginIsDbOwner"] = isOwner is int i && i == 1;
            if (isOwner is int j && j == 1)
                failures.Add("El login SQL del laboratorio es db_owner: debe ser de solo lectura (db_datareader).");
        }
        catch (LabException e) { failures.Add(e.Message); }
        catch (SqlException e) { failures.Add($"SQL: {e.Message}"); }

        return new Report(failures.Count == 0, failures, facts);
    }

    public static void Require(LabConfig cfg)
    {
        var r = Evaluate(cfg);
        if (!r.Ok)
            throw new LabException("GUARD_REFUSED", "Guardas de laboratorio incumplidas: " + string.Join(" | ", r.Failures));
    }
}

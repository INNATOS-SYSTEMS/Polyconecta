using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SdkLab;

internal static partial class SqlTools
{
    [GeneratedRegex(@"^\s*(select|with)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadOnlyStart();

    [GeneratedRegex(@"\b(insert|update|delete|merge|drop|alter|create|truncate|exec|execute|grant|revoke|backup|restore|xp_|sp_)\b|;", RegexOptions.IgnoreCase)]
    private static partial Regex Forbidden();

    public static List<Dictionary<string, object?>> Query(LabConfig cfg, string sql, int maxRows = 500, params (string, object)[] parameters)
    {
        // Defensa en profundidad: el login ya es db_datareader (Guard lo exige).
        if (!ReadOnlyStart().IsMatch(sql) || Forbidden().IsMatch(sql))
            throw new LabException("SQL_NOT_READONLY", "Solo se permite una única sentencia SELECT/WITH.");
        using var conn = Guard.OpenSql(cfg);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;
        foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v);
        using var rd = cmd.ExecuteReader();
        var rows = new List<Dictionary<string, object?>>();
        while (rd.Read() && rows.Count < maxRows)
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < rd.FieldCount; i++)
                row[rd.GetName(i)] = rd.IsDBNull(i) ? null : rd.GetValue(i) is byte[] b ? Convert.ToHexString(b) : rd.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>Existencia por producto/almacén y por capa (lote) para los SKU indicados: la vista independiente que exige la matriz.</summary>
    public static Dictionary<string, object> Snapshot(LabConfig cfg, string[] productCodes)
    {
        if (productCodes.Length == 0) throw new LabException("ARGS", "snapshot requiere al menos un código de producto.");
        var inList = string.Join(",", productCodes.Select((_, i) => $"@p{i}"));
        var ps = productCodes.Select((c, i) => ($"@p{i}", (object)c)).ToArray();

        var exist = Query(cfg, $@"
            SELECT p.CCODIGOPRODUCTO AS producto, a.CCODIGOALMACEN AS almacen, e.CIDEJERCICIO AS ejercicio,
                   (e.CENTRADASINICIALES - e.CSALIDASINICIALES) AS saldoInicial,
                   e.*
            FROM admExistenciaCosto e
            JOIN admProductos p ON p.CIDPRODUCTO = e.CIDPRODUCTO
            JOIN admAlmacenes a ON a.CIDALMACEN = e.CIDALMACEN
            WHERE e.CTIPOEXISTENCIA = 1 AND p.CCODIGOPRODUCTO IN ({inList})", 5000, ps);

        var capas = Query(cfg, $@"
            SELECT p.CCODIGOPRODUCTO AS producto, a.CCODIGOALMACEN AS almacen,
                   c.CIDCAPA, c.CTIPOCAPA, c.CNUMEROLOTE, c.CEXISTENCIA, c.CIDCAPAORIGEN, c.CFECHA
            FROM admCapasProducto c
            JOIN admProductos p ON p.CIDPRODUCTO = c.CIDPRODUCTO
            JOIN admAlmacenes a ON a.CIDALMACEN = c.CIDALMACEN
            WHERE p.CCODIGOPRODUCTO IN ({inList})
            ORDER BY p.CCODIGOPRODUCTO, a.CCODIGOALMACEN, c.CIDCAPA", 5000, ps);

        var almacenes = Query(cfg, "SELECT CIDALMACEN, CCODIGOALMACEN, CNOMBREALMACEN FROM admAlmacenes ORDER BY CIDALMACEN");
        return new() { ["existencias"] = exist, ["capas"] = capas, ["almacenes"] = almacenes };
    }
}

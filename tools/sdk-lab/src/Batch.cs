namespace SdkLab;

/// <summary>
/// S-01: N traspasos (par Salida + Entrada, D-79) en UNA sola sesión del SDK, con la duración de cada llamada.
/// El costo unitario de cada Entrada sale de la Salida recién creada: CCOSTOESPECIFICO ÷ CUNIDADES (regla de D-79, B-03).
/// </summary>
public sealed class PairBatchSpec
{
    public string Id { get; set; } = "";
    public string Producto { get; set; } = "";
    public string AlmacenOrigen { get; set; } = "";
    public string AlmacenDestino { get; set; } = "";
    public double Unidades { get; set; }
    public string ConceptoSalida { get; set; } = "";
    public string ConceptoEntrada { get; set; } = "";
    public string Serie { get; set; } = "";
    public int Repeticiones { get; set; } = 1;
    /// <summary>Prefijo de la referencia; debe empezar con "LAB". Se completa con el número de par y S/E, sin pasar de 20 caracteres (CREFERENCIA).</summary>
    public string Referencia { get; set; } = "";
}

internal static class Batch
{
    public static object RunPairs(LabConfig cfg, PairBatchSpec spec)
    {
        if (!spec.Referencia.StartsWith("LAB", StringComparison.Ordinal))
            throw new LabException("SPEC_INVALID", "referencia debe empezar con 'LAB'.");
        if (spec.Repeticiones is < 1 or > 50)
            throw new LabException("SPEC_INVALID", "repeticiones debe estar entre 1 y 50.");

        var session = new Dictionary<string, long>();
        var pairs = new List<object>();
        string? stopped = null;

        SdkSession.Run(cfg, () =>
        {
            for (var i = 1; i <= spec.Repeticiones; i++)
            {
                var pairWatch = System.Diagnostics.Stopwatch.StartNew();
                var refBase = $"{spec.Referencia}-{i:00}";
                if (refBase.Length + 2 > 20) throw new LabException("SPEC_INVALID", $"La referencia '{refBase}-S' pasa de 20 caracteres.");

                var salida = SpecRunner.RunInSession(Doc(spec, spec.ConceptoSalida, $"{refBase}-S", spec.AlmacenOrigen, 0));
                if (!salida.Completed) { stopped = $"par {i}: Salida en {salida.StoppedAt}"; pairs.Add(new { par = i, salida }); break; }

                var costWatch = System.Diagnostics.Stopwatch.StartNew();
                var mov = SqlTools.Query(cfg, "SELECT CCOSTOESPECIFICO, CUNIDADES FROM admMovimientos WHERE CIDMOVIMIENTO = @id", 1, ("@id", salida.MovIds[0]));
                var costoTotal = Convert.ToDouble(mov[0]["CCOSTOESPECIFICO"]);
                var unidades = Convert.ToDouble(mov[0]["CUNIDADES"]);
                var costoUnitario = unidades == 0 ? 0 : costoTotal / unidades;
                var costMs = costWatch.ElapsedMilliseconds;

                var entrada = SpecRunner.RunInSession(Doc(spec, spec.ConceptoEntrada, $"{refBase}-E", spec.AlmacenDestino, costoUnitario));
                pairs.Add(new { par = i, totalMs = pairWatch.ElapsedMilliseconds, leerCostoMs = costMs, costoUnitario, salida, entrada });
                if (!entrada.Completed) { stopped = $"par {i}: Entrada en {entrada.StoppedAt} (la Salida {salida.DocId} quedó hecha)"; break; }
            }
            return 0;
        }, session);

        var totals = pairs.Select(p => (long?)p.GetType().GetProperty("totalMs")?.GetValue(p)).Where(t => t is not null).Select(t => t!.Value).ToList();
        return new
        {
            specId = spec.Id,
            completed = stopped is null,
            stopped,
            sesion = session,
            paresCompletos = totals.Count,
            parMs = totals.Count == 0 ? null : new { min = totals.Min(), max = totals.Max(), promedio = totals.Average(), primero = totals[0] },
            pares = pairs,
        };
    }

    private static DocSpec Doc(PairBatchSpec s, string concepto, string referencia, string almacen, double costo) => new()
    {
        Id = s.Id, Concepto = concepto, Serie = s.Serie, Referencia = referencia,
        Movimientos = [new MovSpec { Producto = s.Producto, Almacen = almacen, Unidades = s.Unidades, Costo = costo, Referencia = referencia }],
    };
}

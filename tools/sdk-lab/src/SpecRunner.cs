using Contpaq.Bridge.Infrastructure.Sdk;

namespace SdkLab;

public sealed class CapaSpec { public string Lote { get; set; } = ""; public double Unidades { get; set; } public string? Caducidad { get; set; } }

public sealed class MovSpec
{
    public string Producto { get; set; } = "";
    public string Almacen { get; set; } = "";
    public double Unidades { get; set; }
    public double Precio { get; set; }
    public string Referencia { get; set; } = "";
    /// <summary>Cero, una o varias capas. Varias = varias llamadas a fAltaMovimientoSeriesCapas sobre el mismo movimiento (C-02).</summary>
    public List<CapaSpec> Capas { get; set; } = [];
}

/// <summary>
/// Un experimento del SDK descrito como datos. Refleja la secuencia real del bridge:
/// fAltaDocumento → (fAltaMovimiento → fAltaMovimientoSeriesCapas*)* → [fCalculaMovtoSerieCapa] → [fAfectaDocto_Param].
/// </summary>
public sealed class DocSpec
{
    public string Id { get; set; } = "";            // ej. "B-02"; solo etiqueta de evidencia
    public string Concepto { get; set; } = "";       // CCODIGOCONCEPTO
    public string Serie { get; set; } = "";
    public string? Fecha { get; set; }               // se formatea MM/dd/yyyy como el bridge
    public string CodigoCteProv { get; set; } = "";
    public string Referencia { get; set; } = "";     // debe empezar con "LAB" (trazabilidad de lo creado)
    public List<MovSpec> Movimientos { get; set; } = [];
    public bool Calcular { get; set; }               // fCalculaMovtoSerieCapa por movimiento (C-06)
    public bool Afectar { get; set; }                // fAfectaDocto_Param(..., true)
    public bool Desafectar { get; set; }             // fAfectaDocto_Param(..., false) tras afectar (D-04)
}

internal sealed record Step(string Fn, int Rc, string Message, object? Data = null);

internal static class SpecRunner
{
    public static object Run(LabConfig cfg, DocSpec spec)
    {
        if (!spec.Referencia.StartsWith("LAB", StringComparison.Ordinal))
            throw new LabException("SPEC_INVALID", "spec.referencia debe empezar con 'LAB' para poder localizar lo creado.");
        if (spec.Movimientos.Count == 0)
            throw new LabException("SPEC_INVALID", "El documento requiere al menos un movimiento.");

        return SdkSession.Run(cfg, () =>
        {
            var steps = new List<Step>();
            bool Ok(string fn, int rc, object? data = null) { steps.Add(new Step(fn, rc, SdkSession.Msg(rc), data)); return rc == 0; }

            var fecha = DateTime.Now.ToString("MM/dd/yyyy");
            if (!string.IsNullOrWhiteSpace(spec.Fecha))
                fecha = DateTime.TryParse(spec.Fecha, out var dt) ? dt.ToString("MM/dd/yyyy") : spec.Fecha;

            var docId = 0;
            var doc = new tDocumento
            {
                aFolio = 0, aNumMoneda = 1, aTipoCambio = 1.0, aImporte = 0,
                aCodConcepto = spec.Concepto, aSeries = spec.Serie, aFecha = fecha,
                aCodigoCteProv = spec.CodigoCteProv, aCodigoAgente = "",
                aReferencia = spec.Referencia, aObservaciones = $"sdklab {spec.Id}",
            };
            var rc = ContpaqiSdkNative.fAltaDocumento(ref docId, ref doc);
            if (!Ok("fAltaDocumento", rc, new { docId, folio = doc.aFolio })) return Result(spec, steps, docId, doc.aFolio, stoppedAt: "fAltaDocumento");

            var movIds = new List<int>();
            for (var i = 0; i < spec.Movimientos.Count; i++)
            {
                var m = spec.Movimientos[i];
                var movId = 0;
                var mov = new tMovimiento
                {
                    aConsecutivo = 0, aUnidades = m.Unidades, aPrecio = m.Precio, aCosto = 0,
                    aCodProdSer = m.Producto, aCodAlmacen = m.Almacen, aReferencia = m.Referencia, aCodClasific = "",
                };
                rc = ContpaqiSdkNative.fAltaMovimiento(docId, ref movId, ref mov);
                if (!Ok($"fAltaMovimiento[{i}]", rc, new { movId })) return Result(spec, steps, docId, doc.aFolio, stoppedAt: $"fAltaMovimiento[{i}]", orphan: true);
                movIds.Add(movId);

                for (var c = 0; c < m.Capas.Count; c++)
                {
                    var k = m.Capas[c];
                    var capa = new tSeriesCapas
                    {
                        aUnidades = k.Unidades, aTipoCambio = 1.0, aSeries = "", aPedimento = "", aFechaPedimento = "",
                        aAduana = "", aFechaFabricacion = "", aFechaCaducidad = k.Caducidad ?? "", aLote = k.Lote,
                    };
                    rc = ContpaqiSdkNative.fAltaMovimientoSeriesCapas(movId, ref capa);
                    if (!Ok($"fAltaMovimientoSeriesCapas[{i}.{c}]", rc, new { k.Lote, k.Unidades }))
                        return Result(spec, steps, docId, doc.aFolio, stoppedAt: $"fAltaMovimientoSeriesCapas[{i}.{c}]", orphan: true);
                }

                if (spec.Calcular)
                {
                    rc = LabNative.fCalculaMovtoSerieCapa(movId);
                    if (!Ok($"fCalculaMovtoSerieCapa[{i}]", rc)) return Result(spec, steps, docId, doc.aFolio, stoppedAt: $"fCalculaMovtoSerieCapa[{i}]", orphan: true);
                }
            }

            if (spec.Afectar)
            {
                rc = ContpaqiSdkNative.fAfectaDocto_Param(spec.Concepto, spec.Serie, doc.aFolio, true);
                if (!Ok("fAfectaDocto_Param(true)", rc)) return Result(spec, steps, docId, doc.aFolio, stoppedAt: "fAfectaDocto_Param(true)", orphan: true);
            }
            if (spec.Desafectar)
            {
                rc = ContpaqiSdkNative.fAfectaDocto_Param(spec.Concepto, spec.Serie, doc.aFolio, false);
                Ok("fAfectaDocto_Param(false)", rc);
            }
            return Result(spec, steps, docId, doc.aFolio, movIds: movIds);
        });
    }

    private static object Result(DocSpec s, List<Step> steps, int docId, double folio, string? stoppedAt = null, bool orphan = false, List<int>? movIds = null) => new
    {
        specId = s.Id,
        completed = stoppedAt is null,
        stoppedAt,
        // G-01: si falla algo tras crear el documento, queda un documento sin terminar en CONTPAQi.
        orphanDocumentLeftBehind = orphan,
        docId, folio, movIds, steps,
        nextStep = "Verifica el efecto con: sdklab snapshot <productos> y compara contra el snapshot previo. rc=0 NO es evidencia suficiente.",
    };

    public static object Pendientes(LabConfig cfg, string concepto, string producto, string almacen) =>
        SdkSession.Run(cfg, () =>
        {
            var sb = new System.Text.StringBuilder(64);
            var rc = LabNative.fObtieneUnidadesPendientes(concepto, producto, almacen, sb);
            return new { rc, message = SdkSession.Msg(rc), unidades = sb.ToString() };
        });

    public static object Afecta(LabConfig cfg, string concepto, string serie, double folio, bool afecta) =>
        SdkSession.Run(cfg, () =>
        {
            var rc = ContpaqiSdkNative.fAfectaDocto_Param(concepto, serie, folio, afecta);
            return new { rc, message = SdkSession.Msg(rc), concepto, serie, folio, afecta };
        });
}

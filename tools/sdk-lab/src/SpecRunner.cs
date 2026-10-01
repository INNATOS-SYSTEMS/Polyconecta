using Contpaq.Bridge.Infrastructure.Sdk;

namespace SdkLab;

public sealed class CapaSpec { public string Lote { get; set; } = ""; public double Unidades { get; set; } public string? Caducidad { get; set; } }

public sealed class MovSpec
{
    public string Producto { get; set; } = "";
    public string Almacen { get; set; } = "";
    public double Unidades { get; set; }
    public double Precio { get; set; }
    /// <summary>Costo unitario (tMovimiento.aCosto). En una entrada que completa un traspaso, el costo de la salida.</summary>
    public double Costo { get; set; }
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
    public int NumMoneda { get; set; } = 1;          // S-14: 1 = pesos; otra moneda según admMonedas
    public double TipoCambio { get; set; } = 1.0;
}

internal sealed record Step(string Fn, int Rc, string Message, object? Data = null, long Ms = 0);

// OrphanDocumentLeftBehind (G-01): si falla algo tras crear el documento, queda un documento sin terminar en CONTPAQi.
internal sealed record DocResult(string SpecId, bool Completed, string? StoppedAt, bool OrphanDocumentLeftBehind,
    int DocId, double Folio, List<int> MovIds, List<Step> Steps, string NextStep);

internal static class SpecRunner
{
    public static object Run(LabConfig cfg, DocSpec spec)
    {
        if (!spec.Referencia.StartsWith("LAB", StringComparison.Ordinal))
            throw new LabException("SPEC_INVALID", "spec.referencia debe empezar con 'LAB' para poder localizar lo creado.");
        if (spec.Movimientos.Count == 0)
            throw new LabException("SPEC_INVALID", "El documento requiere al menos un movimiento.");

        return SdkSession.Run(cfg, () => RunInSession(spec));
    }

    /// <summary>Crea el documento del spec dentro de una sesión del SDK ya abierta. Cada paso lleva su duración en ms (S-01).</summary>
    public static DocResult RunInSession(DocSpec spec)
    {
        {
            var steps = new List<Step>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool Ok(string fn, int rc, object? data = null) { steps.Add(new Step(fn, rc, SdkSession.Msg(rc), data, sw.ElapsedMilliseconds)); sw.Restart(); return rc == 0; }

            var fecha = DateTime.Now.ToString("MM/dd/yyyy");
            if (!string.IsNullOrWhiteSpace(spec.Fecha))
                fecha = DateTime.TryParse(spec.Fecha, out var dt) ? dt.ToString("MM/dd/yyyy") : spec.Fecha;

            var docId = 0;
            var doc = new tDocumento
            {
                aFolio = 0, aNumMoneda = spec.NumMoneda, aTipoCambio = spec.TipoCambio, aImporte = 0,
                aCodConcepto = spec.Concepto, aSeries = spec.Serie, aFecha = fecha,
                aCodigoCteProv = spec.CodigoCteProv, aCodigoAgente = "",
                aReferencia = spec.Referencia,
            };
            var rc = Progress.Call($"fAltaDocumento {spec.Referencia}", () => ContpaqiSdkNative.fAltaDocumento(ref docId, ref doc));
            if (!Ok("fAltaDocumento", rc, new { docId, folio = doc.aFolio })) return Result(spec, steps, docId, doc.aFolio, stoppedAt: "fAltaDocumento");

            var movIds = new List<int>();
            for (var i = 0; i < spec.Movimientos.Count; i++)
            {
                var m = spec.Movimientos[i];
                var movId = 0;
                var mov = new tMovimiento
                {
                    aConsecutivo = 0, aUnidades = m.Unidades, aPrecio = m.Precio, aCosto = m.Costo,
                    aCodProdSer = m.Producto, aCodAlmacen = m.Almacen, aReferencia = m.Referencia, aCodClasific = "",
                };
                rc = Progress.Call($"fAltaMovimiento[{i}] doc={docId}", () => ContpaqiSdkNative.fAltaMovimiento(docId, ref movId, ref mov));
                if (!Ok($"fAltaMovimiento[{i}]", rc, new { movId })) return Result(spec, steps, docId, doc.aFolio, stoppedAt: $"fAltaMovimiento[{i}]", orphan: true);
                movIds.Add(movId);

                for (var c = 0; c < m.Capas.Count; c++)
                {
                    var k = m.Capas[c];
                    var capa = new tSeriesCapas
                    {
                        aUnidades = k.Unidades, aTipoCambio = 1.0, aSeries = "", aPedimento = "", aFechaPedimento = "",
                        aAgencia = "", aFechaFabricacion = "", aFechaCaducidad = k.Caducidad ?? "", aNumeroLote = k.Lote,
                    };
                    rc = Progress.Call($"fAltaMovimientoSeriesCapas[{i}.{c}] mov={movId}", () => ContpaqiSdkNative.fAltaMovimientoSeriesCapas(movId, ref capa));
                    if (!Ok($"fAltaMovimientoSeriesCapas[{i}.{c}]", rc, new { k.Lote, k.Unidades }))
                        return Result(spec, steps, docId, doc.aFolio, stoppedAt: $"fAltaMovimientoSeriesCapas[{i}.{c}]", orphan: true);
                }

                if (spec.Calcular)
                {
                    rc = Progress.Call("fCalculaMovtoSerieCapa", () => LabNative.fCalculaMovtoSerieCapa(movId));
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
        }
    }

    private static DocResult Result(DocSpec s, List<Step> steps, int docId, double folio, string? stoppedAt = null, bool orphan = false, List<int>? movIds = null) =>
        new(s.Id, stoppedAt is null, stoppedAt, orphan, docId, folio, movIds ?? [], steps,
            "Verifica el efecto con: sdklab snapshot <productos> y compara contra el snapshot previo. rc=0 NO es evidencia suficiente.");

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

    /// <summary>S-08: posiciona el documento por concepto, serie y folio, y lo borra.</summary>
    public static object Borra(LabConfig cfg, string concepto, string serie, string folio) =>
        SdkSession.Run(cfg, () =>
        {
            var rcBusca = Progress.Call($"fBuscarDocumento {concepto}/{serie}/{folio}", () => LabNativeDocs.fBuscarDocumento(concepto, serie, folio));
            if (rcBusca != 0) return (object)new { rcBusca, message = SdkSession.Msg(rcBusca) };
            var rcBorra = Progress.Call("fBorraDocumento", LabNativeDocs.fBorraDocumento);
            return new { rcBusca, rcBorra, message = SdkSession.Msg(rcBorra) };
        });

    /// <summary>S-13: fija un campo de un movimiento existente (p. ej. CIDMOVTOORIGEN de la remisión = movimiento del pedido).</summary>
    public static object SetDatoMovimiento(LabConfig cfg, int idDocumento, int idMovimiento, string campo, string valor) =>
        SdkSession.Run(cfg, () =>
        {
            var steps = new List<object>();
            int Step(string fn, Func<int> f) { var rc = Progress.Call(fn, f); steps.Add(new { fn, rc, message = SdkSession.Msg(rc) }); return rc; }
            // Sin posicionar antes el documento, fBuscarIdMovimiento termina en violación de acceso (S-13, 1-oct).
            if (Step($"fBuscarIdDocumento {idDocumento}", () => LabNativeDocs.fBuscarIdDocumento(idDocumento)) != 0) return (object)steps;
            if (Step($"fBuscarIdMovimiento {idMovimiento}", () => LabNativeDocs.fBuscarIdMovimiento(idMovimiento)) != 0) return (object)steps;
            if (Step("fEditarMovimiento", LabNativeDocs.fEditarMovimiento) != 0) return steps;
            if (Step($"fSetDatoMovimiento {campo}={valor}", () => LabNativeDocs.fSetDatoMovimiento(campo, valor)) != 0) return steps;
            Step("fGuardaMovimiento", LabNativeDocs.fGuardaMovimiento);
            return steps;
        });

    /// <summary>S-09: alta de almacén por SDK (funciones exportadas no documentadas en la referencia).</summary>
    public static object AltaAlmacen(LabConfig cfg, string codigo, string nombre) =>
        SdkSession.Run(cfg, () =>
        {
            if (!codigo.StartsWith("LAB", StringComparison.Ordinal)) throw new LabException("SPEC_INVALID", "El código del almacén de prueba debe empezar con 'LAB'.");
            var steps = new List<object>();
            int Step(string fn, Func<int> f) { var rc = Progress.Call(fn, f); steps.Add(new { fn, rc, message = SdkSession.Msg(rc) }); return rc; }
            if (Step("fInsertaAlmacen", LabNativeDocs.fInsertaAlmacen) != 0) return (object)steps;
            if (Step("fSetDatoAlmacen CCODIGOALMACEN", () => LabNativeDocs.fSetDatoAlmacen("CCODIGOALMACEN", codigo)) != 0) return steps;
            if (Step("fSetDatoAlmacen CNOMBREALMACEN", () => LabNativeDocs.fSetDatoAlmacen("CNOMBREALMACEN", nombre)) != 0) return steps;
            Step("fGuardaAlmacen", LabNativeDocs.fGuardaAlmacen);
            return steps;
        });

    /// <summary>S-17: posiciona el documento por concepto, serie y folio, y lo cancela.</summary>
    public static object Cancela(LabConfig cfg, string concepto, string serie, string folio) =>
        SdkSession.Run(cfg, () =>
        {
            var rcBusca = Progress.Call($"fBuscarDocumento {concepto}/{serie}/{folio}", () => LabNativeDocs.fBuscarDocumento(concepto, serie, folio));
            if (rcBusca != 0) return (object)new { rcBusca, message = SdkSession.Msg(rcBusca) };
            var rcCancela = Progress.Call("fCancelaDocumento", LabNativeDocs.fCancelaDocumento);
            return new { rcBusca, rcCancela, message = SdkSession.Msg(rcCancela) };
        });
}

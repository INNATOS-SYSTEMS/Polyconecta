namespace PolyConecta.Presentation.Services;

/// <summary>Clasificación de producto: filtro y agrupación del Inventario Actual.</summary>
public enum ProductClass
{
    Bolsa,
    RolloImpreso,
    RolloLiso,
    RolloMaestro,
    MateriaPrima,
    Scrap
}

public enum LotStatus { Libre, Reservado, EnWip, Cuarentena }

public class ProductRef
{
    public string Clave { get; set; } = "";
    public string Nombre { get; set; } = "";
    public ProductClass Clasificacion { get; set; }
    public string Unidad { get; set; } = "KGS";
}

/// <summary>Un lote concreto en una ubicación concreta. La reserva es por lote, no por cantidad agregada.</summary>
public class LotBalance
{
    public string Lote { get; set; } = "";
    public string Clave { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public LotStatus Estado { get; set; } = LotStatus.Libre;
    /// <summary>Folio del documento que lo tiene comprometido (pedido u OF), si aplica.</summary>
    public string? ComprometidoPor { get; set; }
}

/// <summary>Renglón del Inventario Actual: la existencia de un lote en una ubicación.</summary>
public record StockQuant(ProductRef Producto, string Ubicacion, string Lote, decimal Cantidad);

public class InventoryState
{
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public const string AlmacenMateriaPrima = "PIM/Stock/MP";
    public const string WipPim = "PIM/WIP";
    public const string WipStc = "SC/WIP";

    public static string ClassLabel(ProductClass c) => c switch
    {
        ProductClass.Bolsa => "Bolsas",
        ProductClass.RolloImpreso => "Rollos impresos",
        ProductClass.RolloLiso => "Rollos sin imprimir",
        ProductClass.RolloMaestro => "Rollos maestros",
        ProductClass.MateriaPrima => "Materia prima",
        _ => "Scrap"
    };

    /// <summary>Ubicaciones excluidas del material vendible (SPEC-007 FR-011).</summary>
    public static bool EsVendible(string ubicacion) =>
        !ubicacion.Contains("Cuarentena", StringComparison.OrdinalIgnoreCase)
        && !ubicacion.Contains("Scrap", StringComparison.OrdinalIgnoreCase);

    // ---------------------------------------------------------------- catálogo

    public List<ProductRef> Catalogo { get; } = new()
    {
        new() { Clave = "PT1113 C567", Nombre = "BOLSA MEDIANA 44X84 C.430 BOL-004 [77]", Clasificacion = ProductClass.Bolsa, Unidad = "MIL" },
        new() { Clave = "PT1113 C580", Nombre = "BOLSA CHICA 38X60 C.380 BOL-002", Clasificacion = ProductClass.Bolsa, Unidad = "MIL" },
        new() { Clave = "PT3413 C4235", Nombre = "ROLLO TUB 20.5 370 (Impreso)", Clasificacion = ProductClass.RolloImpreso, Unidad = "KGS" },
        new() { Clave = "PT3413 C455", Nombre = "ROLLO TUB 20.5 370 (Maestro)", Clasificacion = ProductClass.RolloMaestro, Unidad = "KGS" },
        new() { Clave = "PT3413 C460", Nombre = "ROLLO TUB 20.5 380 (Maestro)", Clasificacion = ProductClass.RolloMaestro, Unidad = "KGS" },
        new() { Clave = "PT3413 C470", Nombre = "ROLLO TUB 22.0 370 (Liso)", Clasificacion = ProductClass.RolloLiso, Unidad = "KGS" },
        new() { Clave = "PACT", Nombre = "PAC TPTE", Clasificacion = ProductClass.MateriaPrima, Unidad = "KGS" },
        new() { Clave = "GA502022", Nombre = "LINEAL BUTENO", Clasificacion = ProductClass.MateriaPrima, Unidad = "KGS" },
        new() { Clave = "MP0032", Nombre = "DESLIZANTE ANTIBLOCKBC110", Clasificacion = ProductClass.MateriaPrima, Unidad = "KGS" },
        new() { Clave = "SCR-BD", Nombre = "Scrap resina residual tpte (BD)", Clasificacion = ProductClass.Scrap, Unidad = "KGS" }
    };

    public ProductRef? GetProducto(string clave) =>
        Catalogo.FirstOrDefault(p => string.Equals(p.Clave, clave?.Trim(), StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- existencias por lote

    public List<LotBalance> Lotes { get; } = new()
    {
        // Materia prima — PIM
        new() { Lote = "PACT-2609A",  Clave = "PACT",     Ubicacion = AlmacenMateriaPrima, Cantidad = 1200m },
        new() { Lote = "PACT-2609B",  Clave = "PACT",     Ubicacion = AlmacenMateriaPrima, Cantidad = 650.5m },
        new() { Lote = "GA-2608",     Clave = "GA502022", Ubicacion = AlmacenMateriaPrima, Cantidad = 400m },
        new() { Lote = "GA-2609",     Clave = "GA502022", Ubicacion = AlmacenMateriaPrima, Cantidad = 220m },
        new() { Lote = "MP32-2609",   Clave = "MP0032",   Ubicacion = AlmacenMateriaPrima, Cantidad = 45.75m },

        // Rollos maestros — PIM. R006 ya comprometido por otro pedido: demuestra físico != disponible.
        new() { Lote = "R004-IV310-26", Clave = "PT3413 C455", Ubicacion = "PIM/Stock/PT", Cantidad = 100m },
        new() { Lote = "R005-IV310-26", Clave = "PT3413 C455", Ubicacion = "PIM/Stock/PT", Cantidad = 100m },
        new() { Lote = "R006-IV310-26", Clave = "PT3413 C455", Ubicacion = "PIM/Stock/PT", Cantidad = 110m, Estado = LotStatus.Reservado, ComprometidoPor = "IV308-26" },
        // Rollo de otra especificación: candidato a sustitución, decisión de AC.
        new() { Lote = "R011-IV295-26", Clave = "PT3413 C460", Ubicacion = "PIM/Stock/PT", Cantidad = 145m },
        new() { Lote = "R012-IV295-26", Clave = "PT3413 C460", Ubicacion = "PIM/Stock/PT", Cantidad = 95m },
        new() { Lote = "R003-IV310-26.S", Clave = "PT3413 C455", Ubicacion = "PIM/Stock/Cuarentena", Cantidad = 105m, Estado = LotStatus.Cuarentena },

        // Rollo liso
        new() { Lote = "R021-IV288-26", Clave = "PT3413 C470", Ubicacion = "PIM/Stock/PT", Cantidad = 260m },

        // Rollos impresos — SC
        new() { Lote = "R001-IV310-26", Clave = "PT3413 C4235", Ubicacion = "SC/Stock/MP", Cantidad = 100m },
        new() { Lote = "R002-IV310-26", Clave = "PT3413 C4235", Ubicacion = "SC/Stock/MP", Cantidad = 80m },

        // Producto terminado — SC
        new() { Lote = "IV310-26-C01", Clave = "PT1113 C567", Ubicacion = "SC/Stock/PT", Cantidad = 2000m },
        new() { Lote = "IV310-26-C02", Clave = "PT1113 C567", Ubicacion = "SC/Stock/PT", Cantidad = 1000m },
        new() { Lote = "IV302-26-C07", Clave = "PT1113 C580", Ubicacion = "SC/Stock/PT", Cantidad = 4500m }
    };

    // ---------------------------------------------------------------- existencias

    public IEnumerable<string> Ubicaciones =>
        Lotes.Select(l => l.Ubicacion).Distinct().OrderBy(u => u);

    public decimal Fisico(string clave, string? ubicacion = null) =>
        LotesDe(clave, ubicacion).Sum(l => l.Cantidad);

    public decimal Reservado(string clave, string? ubicacion = null) =>
        LotesDe(clave, ubicacion).Where(l => l.Estado == LotStatus.Reservado).Sum(l => l.Cantidad);

    public decimal EnWip(string clave, string? ubicacion = null) =>
        LotesDe(clave, ubicacion).Where(l => l.Estado == LotStatus.EnWip).Sum(l => l.Cantidad);

    /// <summary>Físico menos todo lo comprometido. Es la cifra que decide si un pedido requiere proceso.</summary>
    public decimal Disponible(string clave, string? ubicacion = null) =>
        LotesDe(clave, ubicacion).Where(l => l.Estado == LotStatus.Libre && EsVendible(l.Ubicacion)).Sum(l => l.Cantidad);

    public List<LotBalance> LotesDe(string clave, string? ubicacion = null) =>
        Lotes.Where(l => Coincide(l.Clave, clave) && (ubicacion == null || l.Ubicacion == ubicacion)).ToList();

    /// <summary>Cantidad libre de un lote concreto en una ubicación. Usado para pre-validar una recolección.</summary>
    public decimal DisponibleDeLote(string lote, string ubicacion) =>
        Lotes.Where(l => l.Lote == lote && l.Ubicacion == ubicacion && l.Estado == LotStatus.Libre).Sum(l => l.Cantidad);

    public List<LotBalance> LotesDisponibles(string clave, string? ubicacion = null) =>
        LotesDe(clave, ubicacion).Where(l => l.Estado == LotStatus.Libre && EsVendible(l.Ubicacion)).ToList();

    private static bool Coincide(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Existencias del Inventario Actual: un renglón por lote y ubicación.</summary>
    public List<StockQuant> Existencias() => Lotes
        .Where(l => l.Cantidad > 0)
        .Select(l => (Lote: l, Producto: GetProducto(l.Clave)))
        .Where(x => x.Producto != null)
        .Select(x => new StockQuant(x.Producto!, x.Lote.Ubicacion, x.Lote.Lote, x.Lote.Cantidad))
        .ToList();

    // ---------------------------------------------------------------- reservas (SPEC-007 US-4)

    /// <summary>
    /// Reserva lógica: el material sigue en su almacén pero deja de estar disponible.
    /// Se ejecuta al Autorizar el pedido. Distinta de la reserva física de SPEC-008 (recolección a WIP).
    /// </summary>
    public bool Reservar(string lote, string documentoFolio, decimal? cantidad = null)
    {
        var l = Lotes.FirstOrDefault(x => x.Lote == lote);
        if (l == null || l.Estado != LotStatus.Libre) return false;

        // Reasignación de cantidad por lote (FR-009): se parte el lote y el remanente queda disponible.
        if (cantidad is decimal c && c > 0 && c < l.Cantidad)
        {
            Lotes.Add(new LotBalance { Lote = l.Lote, Clave = l.Clave, Ubicacion = l.Ubicacion, Cantidad = l.Cantidad - c });
            l.Cantidad = c;
        }
        l.Estado = LotStatus.Reservado;
        l.ComprometidoPor = documentoFolio;
        Notify();
        return true;
    }

    public void LiberarReservasDe(string documentoFolio)
    {
        foreach (var l in Lotes.Where(x => x.ComprometidoPor == documentoFolio && x.Estado == LotStatus.Reservado))
        {
            l.Estado = LotStatus.Libre;
            l.ComprometidoPor = null;
        }
        Notify();
    }

    // ---------------------------------------------------------------- movimientos WIP (SPEC-008)

    /// <summary>Mueve cantidad de un lote a WIP: reserva física. El material sigue existiendo, ya no está disponible.</summary>
    public bool MoverAWip(string lote, decimal cantidad, string wip, string ofFolio)
    {
        var origen = Lotes.FirstOrDefault(l => l.Lote == lote && l.Estado == LotStatus.Libre);
        if (origen == null || cantidad <= 0 || cantidad > origen.Cantidad) return false;

        origen.Cantidad -= cantidad;
        if (origen.Cantidad <= 0) Lotes.Remove(origen);

        var destino = Lotes.FirstOrDefault(l => l.Lote == lote && l.Ubicacion == wip && l.ComprometidoPor == ofFolio);
        if (destino != null) destino.Cantidad += cantidad;
        else Lotes.Add(new LotBalance { Lote = lote, Clave = origen.Clave, Ubicacion = wip, Cantidad = cantidad, Estado = LotStatus.EnWip, ComprometidoPor = ofFolio });

        Notify();
        return true;
    }

    /// <summary>
    /// Devolución WIP → almacén origen. La cantidad la captura el operador (FR-009b): el sistema no
    /// asume el saldo teórico porque un rollo a medio consumir no pesa lo que el sistema supone.
    /// </summary>
    public bool DevolverDeWip(string lote, decimal cantidad, string wip, string ofFolio, string almacenDestino)
    {
        var enWip = Lotes.FirstOrDefault(l => l.Lote == lote && l.Ubicacion == wip && l.ComprometidoPor == ofFolio);
        if (enWip == null || cantidad <= 0 || cantidad > enWip.Cantidad) return false;

        enWip.Cantidad -= cantidad;
        if (enWip.Cantidad <= 0) Lotes.Remove(enWip);

        var destino = Lotes.FirstOrDefault(l => l.Lote == lote && l.Ubicacion == almacenDestino && l.Estado == LotStatus.Libre);
        if (destino != null) destino.Cantidad += cantidad;
        else Lotes.Add(new LotBalance { Lote = lote, Clave = enWip.Clave, Ubicacion = almacenDestino, Cantidad = cantidad });

        Notify();
        return true;
    }

    public List<LotBalance> SaldoWip(string ofFolio) =>
        Lotes.Where(l => l.Estado == LotStatus.EnWip && l.ComprometidoPor == ofFolio).ToList();

    public decimal SaldoWipTotal(string ofFolio) => SaldoWip(ofFolio).Sum(l => l.Cantidad);
}

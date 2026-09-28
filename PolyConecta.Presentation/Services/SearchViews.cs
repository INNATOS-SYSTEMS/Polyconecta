namespace PolyConecta.Presentation.Services;

/// <summary>Campo buscable de un modelo: sobre qué se busca al teclear.</summary>
public record SearchField<T>(string Etiqueta, Func<T, string?> Valor);

/// <summary>Filtro predefinido con nombre. `Campo` agrupa filtros de la misma dimensión.</summary>
public record SearchFilter<T>(string Nombre, string Campo, Func<T, bool> Condicion);

/// <summary>Campo por el que se puede agrupar la lista.</summary>
public record SearchGroupBy<T>(string Etiqueta, Func<T, string> Clave);

/// <summary>
/// Vista de búsqueda de un modelo: declarativa y separada de la pantalla.
/// Ninguna lista codifica sus propios campos de búsqueda.
/// </summary>
public class SearchView<T>
{
    public List<SearchField<T>> Campos { get; init; } = new();
    public List<SearchFilter<T>> Filtros { get; init; } = new();
    public List<SearchGroupBy<T>> Agrupaciones { get; init; } = new();

    /// <summary>Etiquetas de las agrupaciones con las que abre la lista, en orden de anidamiento.</summary>
    public List<string> AgrupacionesPorDefecto { get; init; } = new();

    /// <summary>Campo de referencia: el respaldo cuando no hay campos declarados.</summary>
    public Func<T, string?>? Referencia { get; init; }

    public string EtiquetasCampos => string.Join(", ", Campos.Select(c => c.Etiqueta));

    /// <summary>
    /// Aplica texto libre y facetas. Facetas del mismo campo se combinan con O;
    /// de campos distintos, con Y.
    /// </summary>
    public List<T> Aplicar(IEnumerable<T> items, string? texto, IEnumerable<string> filtrosActivos)
    {
        var res = items;

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var selectores = Campos.Count > 0
                ? Campos.Select(c => c.Valor).ToList()
                : Referencia != null ? new List<Func<T, string?>> { Referencia } : new();

            res = res.Where(i => selectores.Any(sel =>
                (sel(i) ?? "").Contains(texto, StringComparison.OrdinalIgnoreCase)));
        }

        var activos = filtrosActivos?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
        foreach (var grupo in Filtros.Where(f => activos.Contains(f.Nombre)).GroupBy(f => f.Campo))
        {
            var delGrupo = grupo.ToList();                       // O dentro del campo
            res = res.Where(i => delGrupo.Any(f => f.Condicion(i)));   // Y entre campos
        }

        return res.ToList();
    }
}

/// <summary>
/// Registro central de vistas de búsqueda. Cambiar aquí los campos o filtros de un modelo
/// no requiere tocar su pantalla.
/// </summary>
public static class SearchViews
{
    public static readonly SearchView<ManufacturingOrder> Fabricacion = new()
    {
        Referencia = o => o.Folio,
        Campos =
        {
            new("Folio", o => o.Folio),
            new("Producto", o => o.Producto),
            new("Proceso", o => o.ProcessLabel),
            new("Pedido", o => o.PedidoFolio)
        },
        Filtros =
        {
            new("Borrador", "Estado", o => o.State == "Borrador"),
            new("Planeado", "Estado", o => o.State == "Planeado"),
            new("En progreso", "Estado", o => o.State == "En progreso"),
            new("Hecho", "Estado", o => o.State == "Hecho"),
            new("Extrusión", "Proceso", o => o.ProcessType == "Extrusion"),
            new("Impresión", "Proceso", o => o.ProcessType == "Impresion"),
            new("Bolseo", "Proceso", o => o.ProcessType == "Bolseo"),
            new("Órdenes maestras", "Jerarquía", o => o.OriginFolio == null)
        },
        Agrupaciones =
        {
            new("Estado", o => o.State),
            new("Proceso", o => o.ProcessLabel),
            new("Pedido", o => o.PedidoFolio)
        }
    };

    public static readonly SearchView<StockOperation> Operaciones = new()
    {
        Referencia = o => o.Folio,
        Campos =
        {
            new("Folio", o => o.Folio),
            new("Operación", o => o.Operacion),
            new("Orden", o => o.OfFolio),
            new("Origen", o => o.Origen),
            new("Destino", o => o.Destino)
        },
        Filtros =
        {
            new("Borrador", "Estado", o => o.State == "Borrador"),
            new("En espera", "Estado", o => o.State == "En espera"),
            new("Listo", "Estado", o => o.State == "Listo"),
            new("Hecho", "Estado", o => o.State == "Hecho"),
            new("Recolecciones", "Tipo", o => !o.Tipo.EsDevolucion),
            new("Devoluciones", "Tipo", o => o.Tipo.EsDevolucion),
            new("Backorders", "Origen del documento", o => o.BackorderDe != null),
            new("Pendientes de surtir", "Avance", o => o.EsParcial)
        },
        Agrupaciones =
        {
            new("Estado", o => o.State),
            new("Tipo de operación", o => o.Tipo.Nombre),
            new("Orden de fabricación", o => o.OfFolio)
        }
    };

    /// <summary>Vista del pedido de venta. Se declara sobre una proyección ligera de la lista.</summary>
    public record SalesOrderRow(string Folio, string Cliente, string Producto, string Estado);

    public static readonly SearchView<SalesOrderRow> Pedidos = new()
    {
        Referencia = p => p.Folio,
        Campos =
        {
            new("Folio", p => p.Folio),
            new("Cliente", p => p.Cliente),
            new("Producto", p => p.Producto)
        },
        Filtros =
        {
            new("Borrador", "Estado", p => p.Estado == "Borrador"),
            new("Confirmado", "Estado", p => p.Estado == "Confirmado"),
            new("Autorizado", "Estado", p => p.Estado == "Autorizado"),
            new("En progreso", "Estado", p => p.Estado == "En progreso"),
            new("Hecho", "Estado", p => p.Estado == "Hecho")
        },
        Agrupaciones = { new("Estado", p => p.Estado), new("Cliente", p => p.Cliente) }
    };

    /// <summary>Control de calidad: se lista sobre las órdenes que lo requieren.</summary>
    public static readonly SearchView<ManufacturingOrder> Calidad = new()
    {
        Referencia = o => o.Folio,
        Campos =
        {
            new("Orden", o => o.Folio),
            new("Producto", o => o.Producto),
            new("Proceso", o => o.ProcessLabel)
        },
        Filtros =
        {
            new("Con lotes en revisión", "Resultado", o => o.Produccion.Any(l => l.Estado == "En revisión")),
            new("Totalmente aprobados", "Resultado", o => o.Produccion.Count > 0 && o.Produccion.All(l => l.Estado == "Aprobado")),
            new("Con rechazos", "Resultado", o => o.Produccion.Any(l => l.Estado == "Rechazado")),
            new("Sin producción capturada", "Avance", o => o.Produccion.Count == 0),
            new("Extrusión", "Proceso", o => o.ProcessType == "Extrusion"),
            new("Impresión", "Proceso", o => o.ProcessType == "Impresion"),
            new("Bolseo", "Proceso", o => o.ProcessType == "Bolseo")
        },
        Agrupaciones = { new("Proceso", o => o.ProcessLabel), new("Estado", o => o.State) }
    };

    /// <summary>Proyección común de las operaciones logísticas de documento único.</summary>
    public record LogisticsRow(string Folio, string Operacion, string Origen, string Destino, string Estado);

    private static SearchView<LogisticsRow> Logistica(params string[] estados) => new()
    {
        Referencia = l => l.Folio,
        Campos =
        {
            new("Folio", l => l.Folio),
            new("Operación", l => l.Operacion),
            new("Origen", l => l.Origen),
            new("Destino", l => l.Destino)
        },
        Filtros = estados.Select(e => new SearchFilter<LogisticsRow>(e, "Estado", l => l.Estado == e)).ToList(),
        Agrupaciones = { new("Estado", l => l.Estado), new("Destino", l => l.Destino) }
    };

    public static readonly SearchView<LogisticsRow> Traslados =
        Logistica("Borrador", "En espera de operación", "En espera", "Listo", "Hecho");

    public static readonly SearchView<LogisticsRow> Recepciones =
        Logistica("Borrador", "En espera", "Listo", "Hecho");

    public static readonly SearchView<LogisticsRow> Entregas =
        Logistica("Borrador", "En espera", "Listo", "Hecho");

    public static readonly SearchView<Incidencia> Incidencias = new()
    {
        Referencia = i => i.CentroTrabajo,
        Campos =
        {
            new("Centro de trabajo", i => i.CentroTrabajo),
            new("Tipo", i => i.Tipo),
            new("Comentarios", i => i.Comentarios)
        },
        Filtros =
        {
            new("Extrusión", "Centro", i => i.CentroTrabajo.StartsWith("EXT", StringComparison.OrdinalIgnoreCase) || i.CentroTrabajo.StartsWith("COEXT", StringComparison.OrdinalIgnoreCase)),
            new("Impresión", "Centro", i => i.CentroTrabajo.StartsWith("IMP", StringComparison.OrdinalIgnoreCase)),
            new("Bolseo", "Centro", i => i.CentroTrabajo.StartsWith("BOL", StringComparison.OrdinalIgnoreCase)),
            new("Hoy", "Fecha", i => i.Fecha.Date == new DateTime(2026, 9, 23)),
            new("Esta semana", "Fecha", i => i.Fecha.Date >= new DateTime(2026, 9, 21))
        },
        Agrupaciones = { new("Centro de trabajo", i => i.CentroTrabajo), new("Tipo", i => i.Tipo) }
    };

    /// <summary>Inventario Actual: existencias por lote, agrupadas por ubicación y producto.</summary>
    public static readonly SearchView<StockQuant> InventarioActual = new()
    {
        Referencia = q => q.Producto.Clave,
        Campos =
        {
            new("Producto", q => q.Producto.Clave + " " + q.Producto.Nombre),
            new("Ubicación", q => q.Ubicacion),
            new("Lote", q => q.Lote)
        },
        Filtros = Enum.GetValues<ProductClass>()
            .Select(c => new SearchFilter<StockQuant>(InventoryState.ClassLabel(c), "Clasificación", q => q.Producto.Clasificacion == c))
            .Concat(new SearchFilter<StockQuant>[]
            {
                new("PIM", "Planta", q => q.Ubicacion.StartsWith("PIM", StringComparison.OrdinalIgnoreCase)),
                new("Santa Cruz", "Planta", q => q.Ubicacion.StartsWith("SC", StringComparison.OrdinalIgnoreCase))
            })
            .ToList(),
        Agrupaciones =
        {
            new("Ubicación", q => q.Ubicacion),
            new("Producto", q => q.Producto.Clave + " " + q.Producto.Nombre),
            new("Clasificación", q => InventoryState.ClassLabel(q.Producto.Clasificacion)),
            new("Lote", q => q.Lote)
        },
        AgrupacionesPorDefecto = { "Ubicación", "Producto" }
    };
}

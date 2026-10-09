using System.Linq.Expressions;
using PolyConecta.Application.Common;

namespace PolyConecta.Application.Common.Listas;

public sealed class ColumnaVista<T>
{
    public required string Campo { get; init; }
    public required string Etiqueta { get; init; }
    public bool Ordenable { get; init; }
    public bool Sumable { get; init; }
    public string? Tipo { get; init; }
    public Expression<Func<T, object?>>? Selector { get; init; }
    public Expression<Func<T, decimal>>? Suma { get; init; }
    public Func<FiltroLista, Expression<Func<T, bool>>?>? Filtro { get; init; }
    public Func<T, string>? Unidad { get; init; }
}

public sealed class CampoBuscable<T>
{
    public required string Campo { get; init; }
    public required string Etiqueta { get; init; }
    public required Expression<Func<T, string?>> Expresion { get; init; }
}

public sealed class FiltroNombrado<T>
{
    public required string Nombre { get; init; }
    public required string Campo { get; init; }
    public required Func<ICurrentUser, Expression<Func<T, bool>>> Condicion { get; init; }
    public Func<T, ICurrentUser, bool>? Evaluador { get; init; }
}

public sealed class AgrupacionVista<T>
{
    public required string Campo { get; init; }
    public required string Etiqueta { get; init; }
    public required Expression<Func<T, string?>> Clave { get; init; }
    public Func<T, string?>? EtiquetaGrupo { get; init; }
    public Func<string, Expression<Func<T, bool>>>? FiltroValor { get; init; }
}

public sealed class VistaDeBusqueda<T> where T : class
{
    public required string Modulo { get; init; }
    public required string Lista { get; init; }
    public required string Llave { get; init; }
    public required string PermisoLectura { get; init; }

    public required IReadOnlyList<ColumnaVista<T>> Columnas { get; init; }
    public required IReadOnlyList<CampoBuscable<T>> Campos { get; init; }
    public required IReadOnlyList<FiltroNombrado<T>> Filtros { get; init; }
    public required IReadOnlyList<AgrupacionVista<T>> Agrupaciones { get; init; }
    public IReadOnlyList<string> AgrupacionesPorDefecto { get; init; } = [];
    public IReadOnlyList<OrdenLista> OrdenPorDefecto { get; init; } = [];

    public required Func<T, IReadOnlyDictionary<string, object?>> Proyector { get; init; }

    /// <summary>Devuelve el DTO público para GET …/vista (contracts/api-listas.md).</summary>
    public VistaDeBusquedaDto DescribirVista() => new(
        Lista: Llave,
        Columnas: Columnas.Select(c => new ColumnaVistaDto(c.Campo, c.Etiqueta, c.Ordenable, c.Sumable, c.Tipo)).ToList(),
        Campos: Campos.Select(c => new CampoBuscableDto(c.Campo, c.Etiqueta)).ToList(),
        Filtros: Filtros.Select(f => new FiltroNombradoDto(f.Nombre, f.Campo)).ToList(),
        Agrupaciones: Agrupaciones.Select(a => new AgrupacionVistaDto(a.Etiqueta, a.Campo)).ToList(),
        AgrupacionesPorDefecto: AgrupacionesPorDefecto
    );

    /// <summary>Evalúa los filtros con nombre sobre una entidad en memoria para el conjunto (`_filtros`).</summary>
    public IReadOnlyList<string> EvaluarFiltros(T entidad, ICurrentUser usuario)
    {
        var cumplidos = new List<string>();
        foreach (var f in Filtros)
        {
            if (f.Evaluador is not null)
            {
                if (f.Evaluador(entidad, usuario)) cumplidos.Add(f.Nombre);
            }
            else
            {
                // Compila la condición si no se proveyó evaluador específico.
                var pred = f.Condicion(usuario).Compile();
                if (pred(entidad)) cumplidos.Add(f.Nombre);
            }
        }
        return cumplidos;
    }

    /// <summary>Proyecta una entidad a diccionario de campos para la respuesta HTTP.</summary>
    public IReadOnlyDictionary<string, object?> ProyectarFila(T entidad, ICurrentUser? usuario = null, bool incluirFiltros = false)
    {
        var dict = new Dictionary<string, object?>(Proyector(entidad), StringComparer.Ordinal);
        if (incluirFiltros && usuario is not null)
        {
            dict["_filtros"] = EvaluarFiltros(entidad, usuario);
        }
        return dict;
    }

    /// <summary>Valida la petición según las reglas de contracts/api-listas.md (400 ante campos o valores inválidos).</summary>
    public void Validar(ConsultaLista c)
    {
        var errores = new List<ErrorValidacion>();

        if (c.Pagina < 0)
        {
            errores.Add(new("pagina", "La página debe ser mayor o igual a 0."));
        }

        if (c.Tamano is not (20 or 40 or 80 or 200))
        {
            errores.Add(new("tamano", "El tamaño de página debe ser 20, 40, 80 o 200."));
        }

        foreach (var ord in c.OrdenEfectivo)
        {
            var col = Columnas.FirstOrDefault(col => col.Campo.Equals(ord.Campo, StringComparison.OrdinalIgnoreCase));
            if (col is null || !col.Ordenable)
            {
                errores.Add(new("orden", $"La columna '{ord.Campo}' no existe o no es ordenable."));
            }
        }

        foreach (var f in c.FiltrosEfectivos)
        {
            if (f.Operador is not ("contiene" or "igual" or "entre" or "en"))
            {
                errores.Add(new("filtros", $"El operador '{f.Operador}' no es válido."));
            }

            var existeCampo = Columnas.Any(col => col.Campo.Equals(f.Campo, StringComparison.OrdinalIgnoreCase))
                           || Campos.Any(cp => cp.Campo.Equals(f.Campo, StringComparison.OrdinalIgnoreCase));
            if (!existeCampo)
            {
                errores.Add(new("filtros", $"El campo '{f.Campo}' no es un campo válido para filtrar."));
            }

            if (f.Operador == "entre")
            {
                var (d, h) = f.ComoRango();
                if (d is null && h is null)
                {
                    errores.Add(new("filtros", $"El filtro 'entre' en '{f.Campo}' requiere un rango [desde, hasta]."));
                }
            }
            else if (f.Operador == "en")
            {
                var coleccion = f.ComoColeccion();
                if (coleccion.Count == 0)
                {
                    errores.Add(new("filtros", $"El filtro 'en' en '{f.Campo}' requiere un arreglo de valores."));
                }
            }
        }

        foreach (var nom in c.NombradosEfectivos)
        {
            if (!Filtros.Any(f => f.Nombre.Equals(nom, StringComparison.OrdinalIgnoreCase)))
            {
                errores.Add(new("nombrados", $"El filtro con nombre '{nom}' no existe en la vista."));
            }
        }

        foreach (var ag in c.AgruparPorEfectivo)
        {
            var existe = Agrupaciones.Any(a => a.Campo.Equals(ag, StringComparison.OrdinalIgnoreCase) || a.Etiqueta.Equals(ag, StringComparison.OrdinalIgnoreCase));
            if (!existe)
            {
                errores.Add(new("agruparPor", $"La agrupación '{ag}' no existe en la vista."));
            }
        }

        foreach (var gf in c.GrupoEfectivo)
        {
            var existe = Agrupaciones.Any(a => a.Campo.Equals(gf.Campo, StringComparison.OrdinalIgnoreCase) || a.Etiqueta.Equals(gf.Campo, StringComparison.OrdinalIgnoreCase));
            if (!existe)
            {
                errores.Add(new("grupo", $"El campo de agrupación '{gf.Campo}' no existe en la vista."));
            }
        }

        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }
    }
}

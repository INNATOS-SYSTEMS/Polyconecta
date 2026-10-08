using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PolyConecta.Application.Common;
using PolyConecta.Application.Common.Listas;
using PolyConecta.Application.Plataforma.Seguridad;
using PolyConecta.Domain.Common;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Infrastructure.Persistence.Almacenes;

namespace PolyConecta.Infrastructure.Plataforma.Listas;

/// <summary>
/// Ejecución de consultas de lista y conjuntos completos sobre EF Core (contracts/api-listas.md, D-151).
/// </summary>
public class ConsultaDeListaEf<T> : IConsultaDeLista<T> where T : class
{
    private readonly PolyDbContext _db;
    private readonly VistaDeBusqueda<T> _vista;
    private readonly ReglasDeFila<T> _reglas;
    private readonly IEnumerable<IIncluirEnAlmacen<T>> _inclusiones;
    private readonly IEnumerable<IIncluirEnLista<T>> _inclusionesDeLista;
    private readonly ICurrentUser _usuario;
    private readonly IConfiguration? _configuration;

    public ConsultaDeListaEf(
        PolyDbContext db,
        VistaDeBusqueda<T> vista,
        ReglasDeFila<T> reglas,
        IEnumerable<IIncluirEnAlmacen<T>> inclusiones,
        ICurrentUser usuario,
        IConfiguration? configuration = null,
        IEnumerable<IIncluirEnLista<T>>? inclusionesDeLista = null)
    {
        _db = db;
        _vista = vista;
        _reglas = reglas;
        _inclusiones = inclusiones;
        _inclusionesDeLista = inclusionesDeLista ?? [];
        _usuario = usuario;
        _configuration = configuration;
    }

    private IQueryable<T> AplicarInclusiones(IQueryable<T> query)
    {
        var listaIncs = _inclusionesDeLista.ToList();
        if (listaIncs.Count > 0)
        {
            foreach (var inc in listaIncs) query = inc.Incluir(query);
            return query;
        }

        foreach (var inc in _inclusiones) query = inc.Incluir(query);
        return query;
    }

    public string Modulo => _vista.Modulo;
    public string Lista => _vista.Lista;
    public string Llave => _vista.Llave;
    public string PermisoLectura => _vista.PermisoLectura;
    public VistaDeBusqueda<T> Vista => _vista;

    public VistaDeBusquedaDto DescribirVista() => _vista.DescribirVista();

    public async Task<ResultadoConjunto<IReadOnlyDictionary<string, object?>>> ConjuntoAsync(CancellationToken cancellationToken = default)
    {
        var umbral = (_configuration != null && int.TryParse(_configuration["Listas:Umbral"], out var u)) ? u : 5000;

        IQueryable<T> q = _db.Set<T>().AsNoTracking();
        if (typeof(ArchivableEntity).IsAssignableFrom(typeof(T)))
        {
            q = q.IgnoreQueryFilters();
        }

        var regla = await _reglas.FiltroAsync(cancellationToken);
        if (regla is not null) q = q.Where(regla);

        var total = await q.CountAsync(cancellationToken);
        if (total > umbral)
        {
            return new ResultadoConjunto<IReadOnlyDictionary<string, object?>>(
                Completo: false,
                Total: total,
                Generado: null,
                Filas: null);
        }

        q = AplicarInclusiones(q);
        q = AplicarOrden(q, _vista.OrdenPorDefecto, _vista);

        var entidades = await q.ToListAsync(cancellationToken);
        var filas = entidades.Select(e => _vista.ProyectarFila(e, _usuario, incluirFiltros: true)).ToList();

        return new ResultadoConjunto<IReadOnlyDictionary<string, object?>>(
            Completo: true,
            Total: total,
            Generado: DateTimeOffset.UtcNow,
            Filas: filas);
    }

    public async Task<ResultadoLista<IReadOnlyDictionary<string, object?>>> ConsultarAsync(ConsultaLista consulta, CancellationToken cancellationToken = default)
    {
        _vista.Validar(consulta);

        IQueryable<T> q = _db.Set<T>().AsNoTracking();
        if (consulta.NombradosEfectivos.Contains("Archivados") && typeof(ArchivableEntity).IsAssignableFrom(typeof(T)))
        {
            q = q.IgnoreQueryFilters();
        }

        // 1. Reglas de fila (R-02)
        var regla = await _reglas.FiltroAsync(cancellationToken);
        if (regla is not null) q = q.Where(regla);

        // 2. Filtros con nombre: O dentro del mismo campo, Y entre campos
        q = AplicarFiltrosNombrados(q, _vista, consulta.NombradosEfectivos, _usuario);

        // 3. Filtros por columna: O dentro del mismo campo, Y entre campos
        q = AplicarFiltrosPorColumna(q, _vista, consulta.FiltrosEfectivos);

        // 4. Búsqueda libre: contiene sobre campos buscables unidos con O
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            q = AplicarBusqueda(q, _vista, consulta.Busqueda.Trim());
        }

        // 5. Ruta de grupo
        if (consulta.GrupoEfectivo.Count > 0)
        {
            q = AplicarRutaGrupo(q, _vista, consulta.GrupoEfectivo);
        }

        // 6. Ids seleccionados si vienen
        if (consulta.Ids is not null && consulta.Ids.Count > 0)
        {
            q = AplicarIds(q, consulta.Ids);
        }

        int nivel = consulta.GrupoEfectivo.Count;
        if (nivel < consulta.AgruparPorEfectivo.Count)
        {
            return await ResolverModoAgrupadoAsync(q, consulta, nivel, cancellationToken);
        }
        else
        {
            return await ResolverModoFilasAsync(q, consulta, cancellationToken);
        }
    }

    private async Task<ResultadoLista<IReadOnlyDictionary<string, object?>>> ResolverModoFilasAsync(
        IQueryable<T> q, ConsultaLista consulta, CancellationToken ct)
    {
        var (total, totales) = await CalcularTotalesAsync(q, ct);

        var qFilas = AplicarInclusiones(q);

        var ordenes = consulta.OrdenEfectivo.Count > 0 ? consulta.OrdenEfectivo : _vista.OrdenPorDefecto;
        qFilas = AplicarOrden(qFilas, ordenes, _vista);

        if (consulta.Ids is null || consulta.Ids.Count == 0)
        {
            qFilas = qFilas.Skip(consulta.Pagina * consulta.Tamano).Take(consulta.Tamano);
        }

        var entidades = await qFilas.ToListAsync(ct);
        var filas = entidades.Select(e => _vista.ProyectarFila(e, _usuario, incluirFiltros: false)).ToList();

        return new ResultadoLista<IReadOnlyDictionary<string, object?>>(
            Filas: filas,
            Grupos: null,
            Total: total,
            Totales: totales);
    }

    private async Task<ResultadoLista<IReadOnlyDictionary<string, object?>>> ResolverModoAgrupadoAsync(
        IQueryable<T> q, ConsultaLista consulta, int nivel, CancellationToken ct)
    {
        var nombreAgrupacion = consulta.AgruparPorEfectivo[nivel];
        var agrupacion = _vista.Agrupaciones.First(a =>
            a.Campo.Equals(nombreAgrupacion, StringComparison.OrdinalIgnoreCase) ||
            a.Etiqueta.Equals(nombreAgrupacion, StringComparison.OrdinalIgnoreCase));

        var (_, totalesGenerales) = await CalcularTotalesAsync(q, ct);

        var qGrupos = q.GroupBy(agrupacion.Clave);
        var totalGrupos = await qGrupos.CountAsync(ct);

        var orden = consulta.OrdenEfectivo.FirstOrDefault(o => o.Campo.Equals(agrupacion.Campo, StringComparison.OrdinalIgnoreCase));
        var qOrdenada = (orden is not null && orden.Desc)
            ? qGrupos.OrderByDescending(g => g.Key)
            : qGrupos.OrderBy(g => g.Key);

        var sumables = _vista.Columnas.Where(c => c.Sumable && c.Suma is not null).ToList();
        var colUnidad = _vista.Columnas.FirstOrDefault(c => c.Unidad is not null);

        var pagedGrupos = await qOrdenada
            .Skip(consulta.Pagina * consulta.Tamano)
            .Take(consulta.Tamano)
            .Select(g => new
            {
                Valor = g.Key,
                Cantidad = g.Count(),
            })
            .ToListAsync(ct);

        var grupos = new List<GrupoLista>();
        foreach (var g in pagedGrupos)
        {
            var valorStr = g.Valor ?? string.Empty;
            var subtotales = new Dictionary<string, decimal>();
            Dictionary<string, string>? textos = null;

            if (sumables.Count > 0)
            {
                var pParam = agrupacion.Clave.Parameters[0];
                var eq = Expression.Equal(agrupacion.Clave.Body, Expression.Constant(valorStr, typeof(string)));
                var filtroG = Expression.Lambda<Func<T, bool>>(eq, pParam);
                var qGrupoItems = q.Where(filtroG);

                bool grupoComparteUnidad = true;
                string? unidadComun = null;
                if (colUnidad is not null)
                {
                    var itemsGrupo = await AplicarInclusiones(qGrupoItems).ToListAsync(ct);
                    var unidades = itemsGrupo.Select(colUnidad.Unidad!).Distinct().ToList();
                    grupoComparteUnidad = unidades.Count == 1;
                    if (grupoComparteUnidad) unidadComun = unidades[0];
                }

                if (grupoComparteUnidad)
                {
                    if (unidadComun is not null) textos = new() { ["unidad"] = unidadComun };
                    foreach (var col in sumables)
                    {
                        var suma = await qGrupoItems.SumAsync(col.Suma!, ct);
                        subtotales[col.Campo] = suma;
                    }
                }
            }

            var etiqueta = valorStr == string.Empty ? "Ninguno" : valorStr;
            if (agrupacion.EtiquetaGrupo is not null && valorStr != string.Empty)
            {
                var pParam = agrupacion.Clave.Parameters[0];
                var eq = Expression.Equal(agrupacion.Clave.Body, Expression.Constant(valorStr, typeof(string)));
                var primerItem = await AplicarInclusiones(q.Where(Expression.Lambda<Func<T, bool>>(eq, pParam))).FirstOrDefaultAsync(ct);
                if (primerItem is not null)
                {
                    etiqueta = agrupacion.EtiquetaGrupo(primerItem) ?? valorStr;
                }
            }

            grupos.Add(new GrupoLista(
                Campo: agrupacion.Campo,
                Valor: valorStr,
                Etiqueta: etiqueta,
                Cantidad: g.Cantidad,
                Totales: subtotales,
                Textos: textos));
        }

        return new ResultadoLista<IReadOnlyDictionary<string, object?>>(
            Filas: [],
            Grupos: grupos,
            Total: totalGrupos,
            Totales: totalesGenerales);
    }

    private async Task<(int Total, IReadOnlyDictionary<string, decimal> Totales)> CalcularTotalesAsync(
        IQueryable<T> q, CancellationToken ct)
    {
        var totalCount = await q.CountAsync(ct);
        var sumables = _vista.Columnas.Where(c => c.Sumable && c.Suma is not null).ToList();
        if (sumables.Count == 0)
        {
            return (totalCount, new Dictionary<string, decimal>());
        }

        var dictTotales = new Dictionary<string, decimal>();
        var colUnidad = _vista.Columnas.FirstOrDefault(c => c.Unidad is not null);
        bool comparteUnidad = true;
        if (colUnidad is not null)
        {
            var items = await AplicarInclusiones(q).ToListAsync(ct);
            var unidades = items.Select(colUnidad.Unidad!).Distinct().ToList();
            comparteUnidad = unidades.Count <= 1;
        }

        if (comparteUnidad)
        {
            foreach (var col in sumables)
            {
                var suma = await q.SumAsync(col.Suma!, ct);
                dictTotales[col.Campo] = suma;
            }
        }

        return (totalCount, dictTotales);
    }

    private static IQueryable<T> AplicarFiltrosNombrados(
        IQueryable<T> q, VistaDeBusqueda<T> vista, IReadOnlyList<string> nombrados, ICurrentUser usuario)
    {
        if (nombrados.Count == 0) return q;
        var filtros = nombrados.Select(n => vista.Filtros.First(f => f.Nombre.Equals(n, StringComparison.OrdinalIgnoreCase))).ToList();
        var porCampo = filtros.GroupBy(f => f.Campo);
        Expression<Func<T, bool>>? filtroFinal = null;
        foreach (var grupo in porCampo)
        {
            var exprs = grupo.Select(f => f.Condicion(usuario)).ToList();
            var orGrupo = exprs.Aggregate(Expresiones.O);
            filtroFinal = filtroFinal is null ? orGrupo : Expresiones.Y(filtroFinal, orGrupo);
        }
        return filtroFinal is not null ? q.Where(filtroFinal) : q;
    }

    private static IQueryable<T> AplicarFiltrosPorColumna(
        IQueryable<T> q, VistaDeBusqueda<T> vista, IReadOnlyList<FiltroLista> filtros)
    {
        if (filtros.Count == 0) return q;
        var porCampo = filtros.GroupBy(f => f.Campo);
        Expression<Func<T, bool>>? filtroFinal = null;
        foreach (var grupo in porCampo)
        {
            var exprs = grupo.Select(f => ConstruirFiltroColumna(vista, f)).Where(e => e is not null).Select(e => e!).ToList();
            if (exprs.Count > 0)
            {
                var orGrupo = exprs.Aggregate(Expresiones.O);
                filtroFinal = filtroFinal is null ? orGrupo : Expresiones.Y(filtroFinal, orGrupo);
            }
        }
        return filtroFinal is not null ? q.Where(filtroFinal) : q;
    }

    private static Expression<Func<T, bool>>? ConstruirFiltroColumna(VistaDeBusqueda<T> vista, FiltroLista f)
    {
        var col = vista.Columnas.FirstOrDefault(c => c.Campo.Equals(f.Campo, StringComparison.OrdinalIgnoreCase));
        if (col?.Filtro is not null)
        {
            var res = col.Filtro(f);
            if (res is not null) return res;
        }

        if (col?.Selector is null) return null;

        var selector = col.Selector;
        var param = selector.Parameters[0];
        Expression propExpr = selector.Body;
        if (propExpr is UnaryExpression { NodeType: ExpressionType.Convert } un)
        {
            propExpr = un.Operand;
        }

        var propType = propExpr.Type;
        var nullableUnderlying = Nullable.GetUnderlyingType(propType);
        var targetType = nullableUnderlying ?? propType;

        switch (f.Operador)
        {
            case "igual":
            {
                var valStr = f.ComoTexto();
                if (valStr is null)
                {
                    return Expression.Lambda<Func<T, bool>>(Expression.Equal(propExpr, Expression.Constant(null, propType)), param);
                }
                var valConv = ConvertirValor(valStr, targetType);
                var constVal = Expression.Constant(valConv, targetType);
                Expression targetProp = nullableUnderlying is not null ? Expression.Property(propExpr, "Value") : propExpr;
                Expression comp = Expression.Equal(targetProp, constVal);
                if (nullableUnderlying is not null)
                {
                    comp = Expression.AndAlso(Expression.Property(propExpr, "HasValue"), comp);
                }
                return Expression.Lambda<Func<T, bool>>(comp, param);
            }
            case "contiene":
            {
                var texto = f.ComoTexto() ?? string.Empty;
                if (targetType != typeof(string))
                {
                    return null;
                }
                var notNull = Expression.NotEqual(propExpr, Expression.Constant(null, typeof(string)));
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                var call = Expression.Call(propExpr, containsMethod, Expression.Constant(texto, typeof(string)));
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(notNull, call), param);
            }
            case "en":
            {
                var coleccion = f.ComoColeccion();
                var listaTipada = CrearListaTipada(coleccion, targetType);
                var containsMethod = typeof(ICollection<>).MakeGenericType(targetType).GetMethod("Contains", [targetType])
                                     ?? typeof(List<>).MakeGenericType(targetType).GetMethod("Contains", [targetType])!;
                Expression targetProp = nullableUnderlying is not null ? Expression.Property(propExpr, "Value") : propExpr;
                Expression call = Expression.Call(Expression.Constant(listaTipada), containsMethod, targetProp);
                if (nullableUnderlying is not null)
                {
                    call = Expression.AndAlso(Expression.Property(propExpr, "HasValue"), call);
                }
                return Expression.Lambda<Func<T, bool>>(call, param);
            }
            case "entre":
            {
                var (desde, hasta) = f.ComoRango();
                var desdeConv = desde is not null ? ConvertirValor(desde.ToString()!, targetType) : null;
                var hastaConv = hasta is not null ? ConvertirValor(hasta.ToString()!, targetType) : null;

                Expression targetProp = nullableUnderlying is not null ? Expression.Property(propExpr, "Value") : propExpr;
                Expression? cond = null;

                if (desdeConv is not null)
                {
                    cond = Expression.GreaterThanOrEqual(targetProp, Expression.Constant(desdeConv, targetType));
                }
                if (hastaConv is not null)
                {
                    var le = Expression.LessThanOrEqual(targetProp, Expression.Constant(hastaConv, targetType));
                    cond = cond is null ? le : Expression.AndAlso(cond, le);
                }

                if (cond is null) return null;
                if (nullableUnderlying is not null)
                {
                    cond = Expression.AndAlso(Expression.Property(propExpr, "HasValue"), cond);
                }
                return Expression.Lambda<Func<T, bool>>(cond, param);
            }
            default:
                return null;
        }
    }

    private static object? ConvertirValor(string str, Type targetType)
    {
        if (targetType == typeof(string)) return str;
        if (targetType.IsEnum) return Enum.Parse(targetType, str);
        if (targetType == typeof(DateOnly)) return DateOnly.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        if (targetType == typeof(DateTime)) return DateTime.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        if (targetType == typeof(bool)) return bool.Parse(str);
        if (targetType == typeof(long)) return long.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        if (targetType == typeof(int)) return int.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal)) return decimal.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        return Convert.ChangeType(str, targetType, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static object CrearListaTipada(IEnumerable<object?> items, Type elemType)
    {
        var listType = typeof(List<>).MakeGenericType(elemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
        {
            if (item is null) continue;
            var conv = ConvertirValor(item.ToString()!, elemType);
            list.Add(conv);
        }
        return list;
    }

    private static IQueryable<T> AplicarBusqueda(IQueryable<T> q, VistaDeBusqueda<T> vista, string busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda) || vista.Campos.Count == 0) return q;
        Expression<Func<T, bool>>? orExpr = null;
        foreach (var campo in vista.Campos)
        {
            var p = campo.Expresion.Parameters[0];
            var notNull = Expression.NotEqual(campo.Expresion.Body, Expression.Constant(null, typeof(string)));
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            var containsCall = Expression.Call(campo.Expresion.Body, containsMethod, Expression.Constant(busqueda, typeof(string)));
            var cond = Expression.AndAlso(notNull, containsCall);
            var lambda = Expression.Lambda<Func<T, bool>>(cond, p);

            orExpr = orExpr is null ? lambda : Expresiones.O(orExpr, lambda);
        }
        return orExpr is not null ? q.Where(orExpr) : q;
    }

    private static IQueryable<T> AplicarRutaGrupo(IQueryable<T> q, VistaDeBusqueda<T> vista, IReadOnlyList<GrupoFiltro> grupo)
    {
        foreach (var gf in grupo)
        {
            var ag = vista.Agrupaciones.FirstOrDefault(a =>
                a.Campo.Equals(gf.Campo, StringComparison.OrdinalIgnoreCase) ||
                a.Etiqueta.Equals(gf.Campo, StringComparison.OrdinalIgnoreCase));
            if (ag is null) continue;

            if (ag.FiltroValor is not null)
            {
                q = q.Where(ag.FiltroValor(gf.Valor));
            }
            else
            {
                var p = ag.Clave.Parameters[0];
                var eq = Expression.Equal(ag.Clave.Body, Expression.Constant(gf.Valor, typeof(string)));
                q = q.Where(Expression.Lambda<Func<T, bool>>(eq, p));
            }
        }
        return q;
    }

    private static IQueryable<T> AplicarIds(IQueryable<T> q, IReadOnlyList<string> ids)
    {
        var idsLong = ids.Select(id => long.TryParse(id, out var n) ? n : (long?)null)
            .Where(n => n.HasValue).Select(n => n!.Value).ToList();
        if (idsLong.Count == 0) return q.Where(_ => false);

        var p = Expression.Parameter(typeof(T), "e");
        var idProp = Expression.Property(p, nameof(AuditableEntity.Id));
        var containsMethod = typeof(List<long>).GetMethod(nameof(List<long>.Contains), [typeof(long)])!;
        var call = Expression.Call(Expression.Constant(idsLong), containsMethod, idProp);
        return q.Where(Expression.Lambda<Func<T, bool>>(call, p));
    }

    private static IQueryable<T> AplicarOrden(IQueryable<T> query, IReadOnlyList<OrdenLista> ordenes, VistaDeBusqueda<T> vista)
    {
        if (ordenes.Count == 0) return query;
        bool primero = true;
        foreach (var ord in ordenes)
        {
            var col = vista.Columnas.FirstOrDefault(c => c.Campo.Equals(ord.Campo, StringComparison.OrdinalIgnoreCase));
            if (col?.Selector is null) continue;

            var selector = col.Selector;
            LambdaExpression exprLimpia = selector;
            if (selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            {
                exprLimpia = Expression.Lambda(unary.Operand, selector.Parameters);
            }

            var propType = exprLimpia.ReturnType;
            var metodo = primero
                ? (ord.Desc ? "OrderByDescending" : "OrderBy")
                : (ord.Desc ? "ThenByDescending" : "ThenBy");

            var call = Expression.Call(
                typeof(Queryable),
                metodo,
                [typeof(T), propType],
                query.Expression,
                Expression.Quote(exprLimpia));

            query = query.Provider.CreateQuery<T>(call);
            primero = false;
        }
        return query;
    }
}

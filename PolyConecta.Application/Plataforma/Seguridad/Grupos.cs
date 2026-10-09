using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Chatter;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma.Seguridad;

namespace PolyConecta.Application.Plataforma.Seguridad;

public sealed record GrupoDetalle(
    long Id, string Codigo, string Nombre, string? Descripcion, bool Activo, byte[] RowVersion, int Miembros,
    IReadOnlyList<string> Permisos);

public sealed record AccionDePermiso(string Clave, string Accion, string Etiqueta);

public sealed record ObjetoDePermiso(string Objeto, string Etiqueta, string Tipo, IReadOnlyList<AccionDePermiso> Acciones);

/// <summary>Un nodo de primer nivel del árbol Módulo › Documento o funcionalidad › Acción (D-148).</summary>
public sealed record ModuloDePermiso(string Modulo, string Etiqueta, IReadOnlyList<ObjetoDePermiso> Objetos);

public sealed record ObtenerGrupo(long Id) : IRequierePermiso
{
    public string Permiso => Permisos.GruposLeer;
}

public sealed record ArbolDePermisos : IRequierePermiso
{
    public string Permiso => Permisos.GruposLeer;
}

public sealed record CrearGrupo(string Codigo, string Nombre, string? Descripcion, long? CopiarDe) : IRequierePermiso
{
    public string Permiso => Permisos.GruposAdministrar;
}

/// <summary>Los dos paneles mandan la lista final de claves de permiso del grupo (contracts/api-f1.md).</summary>
public sealed record EditarGrupo(long Id, byte[] RowVersion, string Nombre, string? Descripcion, IReadOnlyList<string> Permisos)
    : IRequierePermiso
{
    public string Permiso => Domain.Plataforma.Seguridad.Permisos.GruposAdministrar;
}

public sealed record ArchivarGrupo(long Id, bool Archivar) : IRequierePermiso
{
    public string Permiso => Permisos.GruposAdministrar;
}

public sealed class DetalleDeGrupo(IAlmacen<User> usuarios, IAlmacen<Permission> permisos)
{
    public async Task<GrupoDetalle> ArmarAsync(Group g, CancellationToken ct)
    {
        var ids = g.Permissions.Select(p => p.PermissionId).ToList();
        var claves = (await permisos.ListarAsync(p => ids.Contains(p.Id), cancellationToken: ct)).Select(p => p.Key).Order(StringComparer.Ordinal).ToList();
        var miembros = await usuarios.ContarAsync(u => u.Assignments.Any(a => a.GroupId == g.Id), cancellationToken: ct);
        return new GrupoDetalle(g.Id, g.Code, g.Name, g.Description, g.IsActive, g.RowVersion, miembros, claves);
    }

    /// <summary>Los permisos como se leen en la bitácora: "Ventas › Pedido › Confirmar" (D-148).</summary>
    public async Task<IReadOnlyList<string>> EtiquetasAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
        (await permisos.ListarAsync(p => ids.Contains(p.Id), cancellationToken: ct)).Select(p => $"{p.ModuleLabel} › {p.ObjectLabel} › {p.Label}").ToList();

    public async Task<IReadOnlyList<long>> IdsAsync(IReadOnlyList<string> claves, CancellationToken ct)
    {
        var todos = await permisos.ListarAsync(cancellationToken: ct);
        var desconocidas = claves.Where(c => todos.All(p => p.Key != c)).ToList();
        if (desconocidas.Count > 0)
            throw new ValidacionException([new ErrorValidacion("permisos", "Permisos desconocidos: " + string.Join(", ", desconocidas))]);
        return todos.Where(p => claves.Contains(p.Key)).Select(p => p.Id).ToList();
    }
}

public sealed class ObtenerGrupoCaso(IAlmacen<Group> grupos, DetalleDeGrupo detalle) : IUseCase<ObtenerGrupo, GrupoDetalle>
{
    public async Task<GrupoDetalle> ExecuteAsync(ObtenerGrupo request, CancellationToken cancellationToken = default) =>
        await detalle.ArmarAsync(await grupos.ObtenerAsync(request.Id, "el grupo", cancellationToken), cancellationToken);
}

/// <summary>El árbol de los dos paneles sale del catálogo: módulo, objeto y acción (D-148).</summary>
public sealed class ArbolDePermisosCaso(IAlmacen<Permission> permisos) : IUseCase<ArbolDePermisos, IReadOnlyList<ModuloDePermiso>>
{
    public async Task<IReadOnlyList<ModuloDePermiso>> ExecuteAsync(ArbolDePermisos request, CancellationToken cancellationToken = default)
    {
        var orden = Domain.Plataforma.Seguridad.Permisos.Catalogo.Select((p, i) => (p.Clave, i)).ToDictionary(x => x.Clave, x => x.i);
        var todos = (await permisos.ListarAsync(cancellationToken: cancellationToken))
            .OrderBy(p => orden.GetValueOrDefault(p.Key, int.MaxValue)).ToList();
        return todos.GroupBy(p => (p.Module, p.ModuleLabel))
            .Select(m => new ModuloDePermiso(m.Key.Module, m.Key.ModuleLabel, m
                .GroupBy(p => (p.Object, p.ObjectLabel, p.ObjectKind))
                .Select(o => new ObjetoDePermiso(o.Key.Object, o.Key.ObjectLabel, o.Key.ObjectKind.ToString(),
                    o.Select(p => new AccionDePermiso(p.Key, p.Action, p.Label)).ToList()))
                .ToList()))
            .ToList();
    }
}

/// <summary>Crea un grupo, vacío o copiando los permisos de otro (US2, escenario 7).</summary>
public sealed class CrearGrupoCaso(IAlmacen<Group> grupos, DetalleDeGrupo detalle, IUnitOfWork uow) : IUseCase<CrearGrupo, GrupoDetalle>
{
    public async Task<GrupoDetalle> ExecuteAsync(CrearGrupo request, CancellationToken cancellationToken = default)
    {
        var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_');
        if (await grupos.ExisteAsync(g => g.Code == codigo, incluirArchivados: true, cancellationToken))
            throw new ReglaDeNegocioException("GRUPO_DUPLICADO", $"Ya existe un grupo con el código {codigo}.");
        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (await grupos.ExisteAsync(g => g.Name == nombre, cancellationToken: cancellationToken))
            throw new ReglaDeNegocioException("GRUPO_DUPLICADO", $"Ya existe un grupo activo llamado {nombre}.");

        var grupo = request.CopiarDe is { } origenId
            ? Group.CopiarDe(await grupos.ObtenerAsync(origenId, "el grupo", cancellationToken), codigo, nombre, request.Descripcion)
            : new Group(codigo, nombre, request.Descripcion);
        grupo.AnotarCambio(request.CopiarDe is null ? "Creó el grupo." : "Creó el grupo copiando los permisos de otro.");
        grupos.Agregar(grupo);
        await uow.SaveChangesAsync(cancellationToken);
        return await detalle.ArmarAsync(grupo, cancellationToken);
    }
}

/// <summary>Guardar los dos paneles: aplica a todos los miembros sin tocar código (US2, escenario 6).</summary>
public sealed class EditarGrupoCaso(IAlmacen<Group> grupos, DetalleDeGrupo detalle, IUnitOfWork uow) : IUseCase<EditarGrupo, GrupoDetalle>
{
    public async Task<GrupoDetalle> ExecuteAsync(EditarGrupo request, CancellationToken cancellationToken = default)
    {
        var grupo = await grupos.ObtenerAsync(request.Id, "el grupo", cancellationToken);
        grupos.ExigirVersion(grupo, request.RowVersion ?? []);
        var (nombre, descripcion) = (grupo.Name, grupo.Description);
        var antes = await detalle.EtiquetasAsync(grupo.Permissions.Select(p => p.PermissionId).ToList(), cancellationToken);
        grupo.Editar(request.Nombre, request.Descripcion);
        var nuevos = await detalle.IdsAsync(request.Permisos ?? [], cancellationToken);
        grupo.AsignarPermisos(nuevos);
        grupo.AnotarCambio(Bitacora.Cambio("Nombre", nombre, grupo.Name) ?? string.Empty);
        grupo.AnotarCambio(Bitacora.Cambio("Descripción", descripcion, grupo.Description) ?? string.Empty);
        foreach (var c in Bitacora.Conjunto(antes, await detalle.EtiquetasAsync(nuevos.ToList(), cancellationToken)))
            grupo.AnotarCambio(c);
        await uow.SaveChangesAsync(cancellationToken);
        return await detalle.ArmarAsync(grupo, cancellationToken);
    }
}

public sealed class ArchivarGrupoCaso(IAlmacen<Group> grupos, IAlmacen<User> usuarios, DetalleDeGrupo detalle, IUnitOfWork uow)
    : IUseCase<ArchivarGrupo, GrupoDetalle>
{
    public async Task<GrupoDetalle> ExecuteAsync(ArchivarGrupo request, CancellationToken cancellationToken = default)
    {
        var grupo = await grupos.ObtenerAsync(request.Id, "el grupo", cancellationToken);
        if (request.Archivar)
            grupo.Archivar(await usuarios.ContarAsync(u => u.Assignments.Any(a => a.GroupId == grupo.Id), cancellationToken: cancellationToken));
        else grupo.Restore();
        grupo.AnotarCambio(request.Archivar ? "Archivó el grupo." : "Restauró el grupo.");
        await uow.SaveChangesAsync(cancellationToken);
        return await detalle.ArmarAsync(grupo, cancellationToken);
    }
}

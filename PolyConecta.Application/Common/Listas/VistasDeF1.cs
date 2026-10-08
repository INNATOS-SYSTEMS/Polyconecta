using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Common.Listas;

/// <summary>
/// Declaración de las cinco vistas de lista de F1 (contracts/api-listas.md).
/// </summary>
public static class VistasDeF1
{
    public static readonly VistaDeBusqueda<SalesOrder> Pedidos = new()
    {
        Modulo = "ventas",
        Lista = "pedidos",
        Llave = "ventas.pedidos",
        PermisoLectura = Permisos.PedidoLeer,
        Columnas =
        [
            new() { Campo = "folio", Etiqueta = "Folio", Ordenable = true, Sumable = false, Selector = o => o.Name },
            new() { Campo = "cliente", Etiqueta = "Cliente", Ordenable = true, Sumable = false, Selector = o => o.Customer.LegalName },
            new() { Campo = "fechaPromesa", Etiqueta = "Entrega estimada", Ordenable = true, Sumable = false, Tipo = "fecha", Selector = o => o.PromiseDate },
            new() { Campo = "estado", Etiqueta = "Estado", Ordenable = true, Sumable = false, Selector = o => o.State.ToString() },
        ],
        Campos =
        [
            new() { Campo = "folio", Etiqueta = "Folio", Expresion = o => o.Name },
            new() { Campo = "cliente", Etiqueta = "Cliente", Expresion = o => o.Customer.LegalName },
        ],
        Filtros =
        [
            new() { Nombre = "Borrador", Campo = "Estado", Condicion = _ => o => o.State == SalesOrderState.Borrador },
            new() { Nombre = "Confirmado", Campo = "Estado", Condicion = _ => o => o.State == SalesOrderState.Confirmado },
            new() { Nombre = "Mis pedidos", Campo = "Responsable", Condicion = u => o => o.CreatedBy == u.UserName, Evaluador = (o, u) => o.CreatedBy == u.UserName },
            new() { Nombre = "Por autorizar", Campo = "Firmas", Condicion = _ => o => o.State == SalesOrderState.Confirmado && o.Signatures.Count < 2, Evaluador = (o, _) => o.State == SalesOrderState.Confirmado && o.Signatures.Count < 2 },
        ],
        Agrupaciones =
        [
            new() { Campo = "estado", Etiqueta = "Estado", Clave = o => o.State.ToString() },
            new() { Campo = "cliente", Etiqueta = "Cliente", Clave = o => o.Customer.ErpCode, EtiquetaGrupo = o => o.Customer != null ? o.Customer.Etiqueta : "" },
        ],
        AgrupacionesPorDefecto = [],
        OrdenPorDefecto = [new("folio", Desc: true)],
        Proyector = o => new Dictionary<string, object?>
        {
            ["id"] = o.Id,
            ["folio"] = o.Name,
            ["cliente"] = o.Customer != null ? o.Customer.Etiqueta : "",
            ["fechaPromesa"] = o.PromiseDate?.ToString("yyyy-MM-dd"),
            ["estado"] = o.State.ToString(),
        },
    };

    public static readonly VistaDeBusqueda<Product> Productos = new()
    {
        Modulo = "inventario",
        Lista = "productos",
        Llave = "inventario.productos",
        PermisoLectura = Permisos.ProductoLeer,
        Columnas =
        [
            new() { Campo = "codigo", Etiqueta = "Clave", Ordenable = true, Sumable = false, Selector = p => p.ErpCode },
            new() { Campo = "nombre", Etiqueta = "Nombre", Ordenable = true, Sumable = false, Selector = p => p.Name },
            new() { Campo = "unidadBase", Etiqueta = "Unidad base", Ordenable = true, Sumable = false, Selector = p => p.ErpUom },
            new() { Campo = "clasificacion", Etiqueta = "Clasificación", Ordenable = true, Sumable = false, Selector = p => p.ClassificationId },
            new() { Campo = "activo", Etiqueta = "Activo", Ordenable = true, Sumable = false, Selector = p => p.IsActive },
        ],
        Campos =
        [
            new() { Campo = "codigo", Etiqueta = "Clave", Expresion = p => p.ErpCode },
            new() { Campo = "nombre", Etiqueta = "Nombre", Expresion = p => p.Name },
        ],
        Filtros =
        [
            new() { Nombre = "Activos", Campo = "Estado", Condicion = _ => p => p.IsActive },
            new() { Nombre = "Archivados", Campo = "Estado", Condicion = _ => p => !p.IsActive },
            new() { Nombre = "Con ficha técnica", Campo = "Ficha técnica", Condicion = _ => p => p.Roll != null && p.Pt != null, Evaluador = (p, _) => p.Roll != null && p.Pt != null },
            new() { Nombre = "Sin ficha técnica", Campo = "Ficha técnica", Condicion = _ => p => p.Roll == null || p.Pt == null, Evaluador = (p, _) => p.Roll == null || p.Pt == null },
        ],
        Agrupaciones =
        [
            new() { Campo = "unidadBase", Etiqueta = "Unidad base", Clave = p => p.ErpUom },
            new() { Campo = "clasificacion", Etiqueta = "Clasificación", Clave = p => p.ClassificationId != null ? p.ClassificationId.ToString()! : "Sin clasificación" },
        ],
        AgrupacionesPorDefecto = [],
        OrdenPorDefecto = [new("codigo", Desc: false)],
        Proyector = p => new Dictionary<string, object?>
        {
            ["id"] = p.Id,
            ["codigo"] = p.ErpCode,
            ["nombre"] = p.Name,
            ["unidadBase"] = p.ErpUom,
            ["clasificacion"] = p.ClassificationId,
            ["activo"] = p.IsActive,
        },
    };

    public static readonly VistaDeBusqueda<Customer> Clientes = new()
    {
        Modulo = "ventas",
        Lista = "clientes",
        Llave = "ventas.clientes",
        PermisoLectura = Permisos.ClienteLeer,
        Columnas =
        [
            new() { Campo = "codigo", Etiqueta = "Código", Ordenable = true, Sumable = false, Selector = c => c.ErpCode },
            new() { Campo = "razonSocial", Etiqueta = "Razón social", Ordenable = true, Sumable = false, Selector = c => c.LegalName },
            new() { Campo = "rfc", Etiqueta = "RFC", Ordenable = true, Sumable = false, Selector = c => c.TaxId },
            new() { Campo = "moneda", Etiqueta = "Moneda", Ordenable = true, Sumable = false, Selector = c => c.Currency },
            new() { Campo = "activo", Etiqueta = "Activo", Ordenable = true, Sumable = false, Selector = c => c.IsActive },
        ],
        Campos =
        [
            new() { Campo = "codigo", Etiqueta = "Código", Expresion = c => c.ErpCode },
            new() { Campo = "razonSocial", Etiqueta = "Razón social", Expresion = c => c.LegalName },
            new() { Campo = "rfc", Etiqueta = "RFC", Expresion = c => c.TaxId },
        ],
        Filtros =
        [
            new() { Nombre = "Activos", Campo = "Estado", Condicion = _ => c => c.IsActive },
            new() { Nombre = "Archivados", Campo = "Estado", Condicion = _ => c => !c.IsActive },
        ],
        Agrupaciones =
        [
            new() { Campo = "moneda", Etiqueta = "Moneda", Clave = c => c.Currency ?? "Sin moneda" },
        ],
        AgrupacionesPorDefecto = [],
        OrdenPorDefecto = [new("codigo", Desc: false)],
        Proyector = c => new Dictionary<string, object?>
        {
            ["id"] = c.Id,
            ["codigo"] = c.ErpCode,
            ["razonSocial"] = c.LegalName,
            ["rfc"] = c.TaxId ?? "",
            ["moneda"] = c.Currency ?? "",
            ["activo"] = c.IsActive,
        },
    };

    public static readonly VistaDeBusqueda<User> Usuarios = new()
    {
        Modulo = "plataforma",
        Lista = "usuarios",
        Llave = "plataforma.usuarios",
        PermisoLectura = Permisos.UsuariosLeer,
        Columnas =
        [
            new() { Campo = "usuario", Etiqueta = "Usuario", Ordenable = true, Sumable = false, Selector = u => u.UserName },
            new() { Campo = "nombre", Etiqueta = "Nombre", Ordenable = true, Sumable = false, Selector = u => u.DisplayName },
            new() { Campo = "email", Etiqueta = "Correo", Ordenable = true, Sumable = false, Selector = u => u.Email },
            new() { Campo = "activo", Etiqueta = "Activo", Ordenable = true, Sumable = false, Selector = u => u.IsActive },
        ],
        Campos =
        [
            new() { Campo = "usuario", Etiqueta = "Usuario", Expresion = u => u.UserName },
            new() { Campo = "nombre", Etiqueta = "Nombre", Expresion = u => u.DisplayName },
            new() { Campo = "email", Etiqueta = "Correo", Expresion = u => u.Email },
        ],
        Filtros =
        [
            new() { Nombre = "Activos", Campo = "Estado", Condicion = _ => u => u.IsActive },
            new() { Nombre = "Archivados", Campo = "Estado", Condicion = _ => u => !u.IsActive },
        ],
        Agrupaciones =
        [
            new() { Campo = "activo", Etiqueta = "Estado", Clave = u => u.IsActive ? "Activo" : "Archivado" },
        ],
        AgrupacionesPorDefecto = [],
        OrdenPorDefecto = [new("usuario", Desc: false)],
        Proyector = u => new Dictionary<string, object?>
        {
            ["id"] = u.Id,
            ["usuario"] = u.UserName,
            ["nombre"] = u.DisplayName,
            ["email"] = u.Email ?? "",
            ["activo"] = u.IsActive,
        },
    };

    public static readonly VistaDeBusqueda<Group> Grupos = new()
    {
        Modulo = "plataforma",
        Lista = "grupos",
        Llave = "plataforma.grupos",
        PermisoLectura = Permisos.GruposLeer,
        Columnas =
        [
            new() { Campo = "codigo", Etiqueta = "Código", Ordenable = true, Sumable = false, Selector = g => g.Code },
            new() { Campo = "nombre", Etiqueta = "Nombre", Ordenable = true, Sumable = false, Selector = g => g.Name },
            new() { Campo = "descripcion", Etiqueta = "Descripción", Ordenable = true, Sumable = false, Selector = g => g.Description },
            new() { Campo = "activo", Etiqueta = "Activo", Ordenable = true, Sumable = false, Selector = g => g.IsActive },
        ],
        Campos =
        [
            new() { Campo = "codigo", Etiqueta = "Código", Expresion = g => g.Code },
            new() { Campo = "nombre", Etiqueta = "Nombre", Expresion = g => g.Name },
            new() { Campo = "descripcion", Etiqueta = "Descripción", Expresion = g => g.Description },
        ],
        Filtros =
        [
            new() { Nombre = "Activos", Campo = "Estado", Condicion = _ => g => g.IsActive },
            new() { Nombre = "Archivados", Campo = "Estado", Condicion = _ => g => !g.IsActive },
        ],
        Agrupaciones =
        [
            new() { Campo = "activo", Etiqueta = "Estado", Clave = g => g.IsActive ? "Activo" : "Archivado" },
        ],
        AgrupacionesPorDefecto = [],
        OrdenPorDefecto = [new("codigo", Desc: false)],
        Proyector = g => new Dictionary<string, object?>
        {
            ["id"] = g.Id,
            ["codigo"] = g.Code,
            ["nombre"] = g.Name,
            ["descripcion"] = g.Description ?? "",
            ["activo"] = g.IsActive,
        },
    };
}

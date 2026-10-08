namespace PolyConecta.Domain.Plataforma.Seguridad;

/// <summary>Un permiso del catálogo en código: clave estable `modulo.objeto.accion` y sus etiquetas.</summary>
public sealed record DefinicionPermiso(
    string Clave, string Modulo, string Objeto, TipoObjeto TipoObjeto, string Accion,
    string EtiquetaModulo, string EtiquetaObjeto, string EtiquetaAccion);

/// <summary>
/// Catálogo cerrado de permisos (R-02, D-148). El código define qué se puede proteger; qué grupo tiene
/// cada permiso es dato (<c>plt.group_permission</c>) y lo edita el Administrador.
/// </summary>
public static class Permisos
{
    public const string PedidoLeer = "ventas.pedido.leer";
    public const string PedidoCrear = "ventas.pedido.crear";
    public const string PedidoEditar = "ventas.pedido.editar";
    public const string PedidoConfirmar = "ventas.pedido.confirmar";
    public const string PedidoCancelar = "ventas.pedido.cancelar";
    public const string PedidoFirmarComercial = "ventas.pedido.firmar_comercial";
    public const string PedidoFirmarCobranza = "ventas.pedido.firmar_cobranza";
    public const string PedidoRevocar = "ventas.pedido.revocar";
    public const string AgenteLeer = "ventas.agente.leer";

    public const string ProductoLeer = "catalogos.producto.leer";
    public const string ProductoClasificar = "catalogos.producto.clasificar";
    public const string FichaLeer = "catalogos.ficha.leer";
    public const string FichaEditar = "catalogos.ficha.editar";
    public const string ClienteLeer = "catalogos.cliente.leer";
    public const string AlmacenLeer = "catalogos.almacen.leer";

    public const string SincronizacionLeer = "plataforma.sincronizacion.leer";
    public const string SincronizacionEjecutar = "plataforma.sincronizacion.ejecutar";
    public const string UsuariosLeer = "plataforma.usuarios.leer";
    public const string UsuariosAdministrar = "plataforma.usuarios.administrar";
    public const string UsuariosLigarAgente = "plataforma.usuarios.ligar_agente";
    public const string GruposLeer = "plataforma.grupos.leer";
    public const string GruposAdministrar = "plataforma.grupos.administrar";

    private static DefinicionPermiso P(string clave, TipoObjeto tipo, string modulo, string objeto, string accion)
    {
        var partes = clave.Split('.');
        return new(clave, partes[0], partes[1], tipo, partes[2], modulo, objeto, accion);
    }

    public static IReadOnlyList<DefinicionPermiso> Catalogo { get; } =
    [
        P(PedidoLeer, TipoObjeto.Documento, "Ventas", "Pedido", "Leer"),
        P(PedidoCrear, TipoObjeto.Documento, "Ventas", "Pedido", "Crear"),
        P(PedidoEditar, TipoObjeto.Documento, "Ventas", "Pedido", "Editar"),
        P(PedidoConfirmar, TipoObjeto.Documento, "Ventas", "Pedido", "Confirmar"),
        P(PedidoCancelar, TipoObjeto.Documento, "Ventas", "Pedido", "Cancelar"),
        P(PedidoFirmarComercial, TipoObjeto.Documento, "Ventas", "Pedido", "Firmar como Comercial"),
        P(PedidoFirmarCobranza, TipoObjeto.Documento, "Ventas", "Pedido", "Firmar como Cobranza"),
        P(PedidoRevocar, TipoObjeto.Documento, "Ventas", "Pedido", "Revocar autorización"),
        P(AgenteLeer, TipoObjeto.Funcionalidad, "Ventas", "Agente de CONTPAQi", "Leer"),

        P(ProductoLeer, TipoObjeto.Funcionalidad, "Catálogos", "Producto", "Leer"),
        P(ProductoClasificar, TipoObjeto.Funcionalidad, "Catálogos", "Producto", "Clasificar"),
        P(FichaLeer, TipoObjeto.Funcionalidad, "Catálogos", "Ficha técnica", "Leer"),
        P(FichaEditar, TipoObjeto.Funcionalidad, "Catálogos", "Ficha técnica", "Editar"),
        P(ClienteLeer, TipoObjeto.Funcionalidad, "Catálogos", "Cliente", "Leer"),
        P(AlmacenLeer, TipoObjeto.Funcionalidad, "Catálogos", "Almacén de CONTPAQi", "Leer"),

        P(SincronizacionLeer, TipoObjeto.Funcionalidad, "Plataforma", "Sincronización", "Leer"),
        P(SincronizacionEjecutar, TipoObjeto.Funcionalidad, "Plataforma", "Sincronización", "Sincronizar ahora"),
        P(UsuariosLeer, TipoObjeto.Funcionalidad, "Plataforma", "Usuarios", "Leer"),
        P(UsuariosAdministrar, TipoObjeto.Funcionalidad, "Plataforma", "Usuarios", "Administrar"),
        P(UsuariosLigarAgente, TipoObjeto.Funcionalidad, "Plataforma", "Usuarios", "Ligar agente de CONTPAQi"),
        P(GruposLeer, TipoObjeto.Funcionalidad, "Plataforma", "Grupos", "Leer"),
        P(GruposAdministrar, TipoObjeto.Funcionalidad, "Plataforma", "Grupos", "Administrar"),
    ];

    public static DefinicionPermiso? Buscar(string clave) => Catalogo.FirstOrDefault(p => p.Clave == clave);
}

/// <summary>
/// Los diez grupos iniciales (los roles de 01 §3) y sus permisos al sembrar (data-model §1, D-148,
/// ajustes de la ratificación: Comercial y Cobranza leen el pedido; el Administrador lee, confirma,
/// cancela y revoca, sin crear ni editar). Solo se siembran si no hay grupos.
/// </summary>
public static class GruposIniciales
{
    public const string AtencionClientes = "ATENCION_CLIENTES";
    public const string Comercial = "COMERCIAL";
    public const string Cobranza = "COBRANZA";
    public const string Planner = "PLANNER";
    public const string Almacenista = "ALMACENISTA";
    public const string Calidad = "CALIDAD";
    public const string Trafico = "TRAFICO";
    public const string Supervisor = "SUPERVISOR_TURNO";
    public const string Administrador = "ADMINISTRADOR";
    public const string Sistemas = "SISTEMAS";

    /// <summary>Lo que leen todos los grupos (data-model §1, "Todos").</summary>
    private static readonly string[] Todos =
    [
        Permisos.AgenteLeer, Permisos.ProductoLeer, Permisos.FichaLeer, Permisos.ClienteLeer, Permisos.AlmacenLeer,
    ];

    public sealed record Definicion(string Codigo, string Nombre, string Descripcion, IReadOnlyList<string> Permisos);

    public static IReadOnlyList<Definicion> Catalogo { get; } =
    [
        new(AtencionClientes, "Atención a Clientes", "Captura, confirma y cancela el pedido",
            [.. Todos, Permisos.PedidoLeer, Permisos.PedidoCrear, Permisos.PedidoEditar, Permisos.PedidoConfirmar,
             Permisos.PedidoCancelar, Permisos.FichaEditar]),
        new(Comercial, "Comercial", "Firma la autorización comercial y la revoca",
            [.. Todos, Permisos.PedidoLeer, Permisos.PedidoFirmarComercial, Permisos.PedidoRevocar]),
        new(Cobranza, "Crédito y Cobranza", "Firma la autorización de crédito y la revoca",
            [.. Todos, Permisos.PedidoLeer, Permisos.PedidoFirmarCobranza, Permisos.PedidoRevocar]),
        new(Planner, "Planner", "Planea y cierra las órdenes de fabricación de su planta", [.. Todos, Permisos.PedidoLeer]),
        new(Almacenista, "Almacenista", "Declara y valida lo que sale o entra del almacén de su planta", [.. Todos]),
        new(Calidad, "Calidad", "Aprueba o rechaza lotes", [.. Todos]),
        new(Trafico, "Logística / Tráfico", "Valida entregas a cliente y salidas a flete", [.. Todos, Permisos.PedidoLeer]),
        new(Supervisor, "Supervisor de turno", "Captura las incidencias", [.. Todos]),
        new(Administrador, "Administrador", "Configura catálogos, usuarios, grupos y suplentes",
            [.. Todos, Permisos.PedidoLeer, Permisos.PedidoConfirmar, Permisos.PedidoCancelar, Permisos.PedidoRevocar,
             Permisos.ProductoClasificar, Permisos.FichaEditar, Permisos.SincronizacionLeer, Permisos.SincronizacionEjecutar,
             Permisos.UsuariosLeer, Permisos.UsuariosAdministrar, Permisos.UsuariosLigarAgente, Permisos.GruposLeer,
             Permisos.GruposAdministrar]),
        new(Sistemas, "Sistemas", "Atiende la sincronización con CONTPAQi",
            [.. Todos, Permisos.SincronizacionLeer, Permisos.SincronizacionEjecutar]),
    ];
}

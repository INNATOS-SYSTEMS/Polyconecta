using System.Globalization;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;

namespace PolyConecta.Domain.Ventas;

/// <summary>Estados del pedido (02 §1). En F1 se usan Borrador, Confirmado, Autorizado y Cancelado.</summary>
public enum SalesOrderState
{
    Borrador,
    Confirmado,
    Autorizado,
    EnProgreso,
    Hecho,
    Cancelado,
}

public enum OrigenPedido
{
    Manual,
    /// <summary>Existe en el modelo, pero no se construye (D-145).</summary>
    Sync,
}

/// <summary>Las dos firmas de la autorización (D-33).</summary>
public enum RolFirma
{
    Comercial,
    Cobranza,
}

/// <summary>Monedas que el bridge sabe traducir y la moneda base (D-146, R-09).</summary>
public sealed record ReglasDeMoneda(string Base, IReadOnlyCollection<string> Admitidas)
{
    public static readonly ReglasDeMoneda PorOmision = new("MXN", ["MXN", "USD"]);
}

/// <summary>Quien hace la operación: la persona y el grupo con el que actúa (CT-32, D-38).</summary>
public sealed record Actor(long UserId, string Nombre, string? Grupo, bool EsSuplente);

/// <summary>Una línea pedida: con <see cref="Id"/> se actualiza, sin él se agrega (contracts/api-f1.md).</summary>
public sealed record LineaSolicitada(
    long? Id, Product Producto, decimal Cantidad, decimal? PrecioUnitario, decimal? MetaProduccionKg, decimal? ToleranciaPorcentaje);

/// <summary>El maestro y las líneas de un guardado (D-136: se guardan juntos).</summary>
public sealed record DatosPedido(
    Customer Cliente, string? OrdenCompra, ErpAgent? Agente, DateOnly FechaPedido, DateOnly? FechaPromesa,
    long? DomicilioEntregaId, string Moneda, decimal? TipoCambio, IReadOnlyList<LineaSolicitada> Lineas);

/// <summary>
/// Pedido de venta libre (04 §3, FR-019 a FR-026). Cambia de estado solo por transiciones con nombre;
/// cada operación, también la firma y la revocación, deja su registro en la bitácora y el chatter (CT-32).
/// </summary>
public sealed class SalesOrder : DocumentoConEstado<SalesOrderState>, ISyncedDocument
{
    public const string TipoDocumento = "PEDIDO_VENTA";

    private readonly List<SalesOrderLine> _lineas = [];
    private readonly List<AuthorizationSignature> _firmas = [];

    /// <summary>Folio `PV-2026-0001` (FR-021). Solo se muestra; las rutas van por id (D-154).</summary>
    public string Name { get; private set; } = string.Empty;

    public OrigenPedido Origin { get; private set; } = OrigenPedido.Manual;

    public long CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public string? CustomerPo { get; private set; }

    public long? AgentId { get; private set; }

    public DateOnly OrderDate { get; private set; }

    /// <summary>Fecha estimada de entrega (D-140).</summary>
    public DateOnly? PromiseDate { get; private set; }

    public long? DeliveryAddressId { get; private set; }

    /// <summary>Copia del domicilio al confirmar: un cambio posterior en CONTPAQi no altera el pedido (R-09).</summary>
    public string? DeliveryAddressText { get; private set; }

    public string Currency { get; private set; } = "MXN";

    public decimal? ExchangeRate { get; private set; }

    public SyncState Sync { get; private set; } = new();

    public IReadOnlyList<SalesOrderLine> Lines => _lineas;

    public IReadOnlyList<AuthorizationSignature> Signatures => _firmas;

    private SalesOrder() : base(SalesOrderState.Borrador) { }

    /// <summary>"Nuevo": el pedido nace libre, en Borrador, con su folio (FR-019, Principio X).</summary>
    public static SalesOrder Crear(string folio, DatosPedido datos, ReglasDeMoneda monedas)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (string.IsNullOrWhiteSpace(folio)) throw new ArgumentException("Falta el folio.", nameof(folio));
        if (!datos.Cliente.IsActive)
            throw new DatosIncompletosException("El cliente está archivado.", [new("clienteId", "El cliente ya no está activo en CONTPAQi.")]);
        var p = new SalesOrder { Name = folio };
        p.Aplicar(datos, monedas);
        p.AgregarTransicion(new TransicionRegistrada("Nuevo", nameof(SalesOrderState.Borrador), "Pedido creado con Nuevo"));
        return p;
    }

    /// <summary>
    /// Edita maestro y líneas (FR-024). Con firmas, guardar revoca la autorización en la misma operación,
    /// solo si quien edita lo confirmó (D-147): borra las firmas y deja Confirmado.
    /// </summary>
    public void Editar(DatosPedido datos, bool revocarAutorizacion, ReglasDeMoneda monedas)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (State is not (SalesOrderState.Borrador or SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", $"Un pedido {Etiqueta(State)} no se edita.");
        if (_firmas.Count > 0 && !revocarAutorizacion)
            throw new ReglaDeNegocioException("EDICION_REVOCA_AUTORIZACION", AvisoDeEdicion!);
        if (datos.Cliente.Id != CustomerId && !datos.Cliente.IsActive)
            throw new DatosIncompletosException("El cliente está archivado.", [new("clienteId", "El cliente ya no está activo en CONTPAQi.")]);

        var anterior = State;
        Aplicar(datos, monedas);
        if (anterior != SalesOrderState.Borrador)
            AgregarTransicion(new TransicionRegistrada(anterior.ToString(), anterior.ToString(), "Pedido editado"));
        if (_firmas.Count > 0)
        {
            _firmas.Clear();
            if (State == SalesOrderState.Autorizado) Transicionar(SalesOrderState.Confirmado, "Revocada por edición", SalesOrderState.Autorizado);
            else AgregarTransicion(new TransicionRegistrada(State.ToString(), State.ToString(), "Revocada por edición"));
        }
        if (State == SalesOrderState.Confirmado) CopiarDomicilio();
    }

    /// <summary>Aviso de D-147 antes de guardar un pedido con firmas.</summary>
    public string? AvisoDeEdicion => _firmas.Count == 0
        ? null
        : $"El pedido tiene {_firmas.Count} {(_firmas.Count == 1 ? "firma" : "firmas")}: guardar un cambio revoca la autorización.";

    /// <summary>Confirmar (FR-022): cliente activo, al menos una línea, cantidad y precio en cada una y tipo de cambio si no es la moneda base.</summary>
    public void Confirmar(ReglasDeMoneda monedas)
    {
        ArgumentNullException.ThrowIfNull(monedas);
        if (State != SalesOrderState.Borrador)
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", "Solo se confirma un pedido en Borrador.");
        var faltantes = Faltantes(monedas);
        if (faltantes.Count > 0)
            throw new DatosIncompletosException("No se puede confirmar: " + string.Join(" ", faltantes.Select(f => f.Mensaje)), faltantes);
        CopiarDomicilio();
        Transicionar(SalesOrderState.Confirmado, null, SalesOrderState.Borrador);
    }

    /// <summary>Lo que le falta al pedido para confirmarse; vacío si está completo.</summary>
    public IReadOnlyList<CampoFaltante> Faltantes(ReglasDeMoneda monedas)
    {
        ArgumentNullException.ThrowIfNull(monedas);
        var f = new List<CampoFaltante>();
        if (Customer is { IsActive: false }) f.Add(new("clienteId", "El cliente ya no está activo en CONTPAQi."));
        if (_lineas.Count == 0) f.Add(new("lineas", "Agrega al menos una línea."));
        for (var i = 0; i < _lineas.Count; i++)
        {
            var l = _lineas[i];
            if (l.RequestedQty <= 0) f.Add(new($"lineas[{i}].cantidad", $"La línea {i + 1} no tiene cantidad."));
            if (l.UnitPrice is null) f.Add(new($"lineas[{i}].precioUnitario", $"La línea {i + 1} no tiene precio."));
        }
        if (Currency != monedas.Base && ExchangeRate is not > 0)
            f.Add(new("tipoCambio", $"Falta el tipo de cambio de {Currency}."));
        return f;
    }

    /// <summary>
    /// Firma de un rol (FR-023, RF-3, RF-4, D-34, D-38). Un rol no firma dos veces y una persona no aporta
    /// las dos firmas; la segunda firma deja el pedido Autorizado.
    /// </summary>
    public void Firmar(Actor actor, RolFirma rol, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (State != SalesOrderState.Confirmado)
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", "Solo se autoriza un pedido Confirmado.");
        if (_firmas.Any(f => f.Role == rol))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", $"Ya está firmado por {rol}.");
        if (_firmas.Any(f => f.UserId == actor.UserId))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", RazonFirmaDoble);

        _firmas.Add(new AuthorizationSignature(rol, actor, ahora));
        var nota = $"Firma de {rol}{(actor.EsSuplente ? " (suplente)" : string.Empty)}, {_firmas.Count} de 2";
        if (_firmas.Count == 2) Transicionar(SalesOrderState.Autorizado, nota, SalesOrderState.Confirmado);
        else AgregarTransicion(new TransicionRegistrada(State.ToString(), State.ToString(), nota));
    }

    public const string RazonFirmaDoble = "Ya firmaste este pedido; la otra firma la da otra persona";

    /// <summary>
    /// Revocar (FR-025, D-33): un firmante del pedido o el Administrador, con motivo. Borra las firmas y
    /// deja Confirmado. Desde F2 se verifica además que ningún documento generado haya avanzado.
    /// </summary>
    public void Revocar(Actor actor, string motivo, bool esAdministrador)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DatosIncompletosException("Falta el motivo.", [new("motivo", "Escribe el motivo de la revocación.")]);
        if (_firmas.Count == 0 || State is not (SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", "El pedido no tiene autorización que revocar.");
        if (!esAdministrador && _firmas.All(f => f.UserId != actor.UserId))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", "Solo revoca quien firmó el pedido o el Administrador.");

        _firmas.Clear();
        var nota = "Autorización revocada: " + motivo.Trim();
        if (State == SalesOrderState.Autorizado) Transicionar(SalesOrderState.Confirmado, nota, SalesOrderState.Autorizado);
        else AgregarTransicion(new TransicionRegistrada(State.ToString(), State.ToString(), nota));
    }

    /// <summary>Cancelar (FR-026): con motivo, desde Borrador, Confirmado o Autorizado. Ya no se edita.</summary>
    public void Cancelar(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DatosIncompletosException("Falta el motivo.", [new("motivo", "Escribe el motivo de la cancelación.")]);
        if (State is not (SalesOrderState.Borrador or SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            throw new ReglaDeNegocioException("TRANSICION_INVALIDA", $"Un pedido {Etiqueta(State)} no se cancela.");
        Transicionar(SalesOrderState.Cancelado, "Cancelado: " + motivo.Trim(),
            SalesOrderState.Borrador, SalesOrderState.Confirmado, SalesOrderState.Autorizado);
    }

    /// <summary>Roles que <paramref name="tiene"/> le deja firmar y que aún no se han firmado.</summary>
    public IReadOnlyList<RolFirma> RolesPorFirmar(Func<string, bool> tiene)
    {
        ArgumentNullException.ThrowIfNull(tiene);
        var roles = new List<RolFirma>();
        if (tiene(Permisos.PedidoFirmarComercial) && _firmas.All(f => f.Role != RolFirma.Comercial)) roles.Add(RolFirma.Comercial);
        if (tiene(Permisos.PedidoFirmarCobranza) && _firmas.All(f => f.Role != RolFirma.Cobranza)) roles.Add(RolFirma.Cobranza);
        return roles;
    }

    /// <summary>
    /// Acciones del pedido para el usuario, con la razón de las que no proceden (CT-26): combina el permiso
    /// (<paramref name="tiene"/>) y la precondición del dominio. La API rechaza igual lo no disponible.
    /// </summary>
    public IReadOnlyList<(string Accion, bool Disponible, string? Razon, string? Aviso)> AccionesDisponibles(
        long userId, Func<string, bool> tiene, bool esAdministrador, ReglasDeMoneda monedas)
    {
        ArgumentNullException.ThrowIfNull(tiene);
        static string SinPermiso(string clave)
        {
            var d = Permisos.Buscar(clave)!;
            return $"Tu grupo no tiene el permiso {d.EtiquetaModulo} › {d.EtiquetaObjeto} › {d.EtiquetaAccion}.";
        }
        (string, bool, string?, string?) Si(string a, string? aviso = null) => (a, true, null, aviso);
        (string, bool, string?, string?) No(string a, string razon) => (a, false, razon, null);

        var acciones = new List<(string, bool, string?, string?)>();

        // Editar
        if (!tiene(Permisos.PedidoEditar)) acciones.Add(No("editar", SinPermiso(Permisos.PedidoEditar)));
        else if (State is not (SalesOrderState.Borrador or SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            acciones.Add(No("editar", $"Un pedido {Etiqueta(State)} no se edita."));
        else acciones.Add(Si("editar", AvisoDeEdicion));

        // Confirmar
        if (!tiene(Permisos.PedidoConfirmar)) acciones.Add(No("confirmar", SinPermiso(Permisos.PedidoConfirmar)));
        else if (State != SalesOrderState.Borrador) acciones.Add(No("confirmar", "Solo se confirma un pedido en Borrador."));
        else acciones.Add(Si("confirmar"));

        // Autorizar: un solo botón para Comercial y Cobranza (FR-023)
        if (!tiene(Permisos.PedidoFirmarComercial) && !tiene(Permisos.PedidoFirmarCobranza))
            acciones.Add(No("autorizar", "Tu grupo no firma la autorización de pedidos."));
        else if (State != SalesOrderState.Confirmado) acciones.Add(No("autorizar", "Solo se autoriza un pedido Confirmado."));
        else if (_firmas.Any(f => f.UserId == userId)) acciones.Add(No("autorizar", RazonFirmaDoble));
        else if (RolesPorFirmar(tiene).Count == 0)
            acciones.Add(No("autorizar", $"Ya está firmado por {string.Join(" y ", _firmas.Select(f => f.Role))}."));
        else acciones.Add(Si("autorizar"));

        // Revocar
        if (!tiene(Permisos.PedidoRevocar)) acciones.Add(No("revocar", SinPermiso(Permisos.PedidoRevocar)));
        else if (_firmas.Count == 0 || State is not (SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            acciones.Add(No("revocar", "El pedido no tiene autorización que revocar."));
        else if (!esAdministrador && _firmas.All(f => f.UserId != userId))
            acciones.Add(No("revocar", "Solo revoca quien firmó el pedido o el Administrador."));
        else acciones.Add(Si("revocar"));

        // Cancelar
        if (!tiene(Permisos.PedidoCancelar)) acciones.Add(No("cancelar", SinPermiso(Permisos.PedidoCancelar)));
        else if (State is not (SalesOrderState.Borrador or SalesOrderState.Confirmado or SalesOrderState.Autorizado))
            acciones.Add(No("cancelar", $"Un pedido {Etiqueta(State)} no se cancela."));
        else acciones.Add(Si("cancelar"));

        return acciones;
    }

    public static string Etiqueta(SalesOrderState estado) => estado switch
    {
        SalesOrderState.EnProgreso => "En progreso",
        _ => estado.ToString(),
    };

    private void Aplicar(DatosPedido d, ReglasDeMoneda monedas)
    {
        var moneda = (d.Moneda ?? string.Empty).Trim().ToUpperInvariant();
        if (!monedas.Admitidas.Contains(moneda))
            throw new DatosIncompletosException("Moneda no admitida.", [new("moneda", $"La moneda {moneda} no está entre las admitidas ({string.Join(", ", monedas.Admitidas)}).")]);
        if (d.TipoCambio is <= 0)
            throw new DatosIncompletosException("Tipo de cambio inválido.", [new("tipoCambio", "El tipo de cambio es mayor que cero.")]);
        if (d.FechaPromesa is { } promesa && promesa < d.FechaPedido)
            throw new DatosIncompletosException("Fecha de entrega inválida.", [new("fechaPromesa", "La entrega estimada no es anterior a la fecha del pedido.")]);

        if (d.DomicilioEntregaId is { } dom
            && !d.Cliente.DomiciliosDeEnvio.Any(a => a.Id == dom)
            && !(dom == DeliveryAddressId && d.Cliente.Id == CustomerId))
            throw new DatosIncompletosException("Domicilio de entrega inválido.", [new("domicilioEntregaId", "El domicilio de entrega es uno de envío del cliente (D-149).")]);

        Customer = d.Cliente;
        CustomerId = d.Cliente.Id;
        CustomerPo = string.IsNullOrWhiteSpace(d.OrdenCompra) ? null : d.OrdenCompra.Trim();
        AgentId = d.Agente?.Id;
        OrderDate = d.FechaPedido;
        PromiseDate = d.FechaPromesa;
        // Con un solo domicilio de envío se propone ese (D-149).
        var envios = d.Cliente.DomiciliosDeEnvio.ToList();
        DeliveryAddressId = d.DomicilioEntregaId ?? (envios.Count == 1 ? envios[0].Id : null);
        Currency = moneda;
        ExchangeRate = moneda == monedas.Base ? 1m : d.TipoCambio;
        AplicarLineas(d.Lineas);
    }

    private void AplicarLineas(IReadOnlyList<LineaSolicitada> lineas)
    {
        var errores = new List<CampoFaltante>();
        var conservadas = lineas.Where(l => l.Id is not null).Select(l => l.Id!.Value).ToHashSet();
        foreach (var desconocida in conservadas.Where(id => _lineas.All(l => l.Id != id)))
            errores.Add(new("lineas", $"La línea {desconocida.ToString(CultureInfo.InvariantCulture)} no es de este pedido."));
        for (var i = 0; i < lineas.Count; i++)
        {
            var l = lineas[i];
            if (l.Cantidad < 0) errores.Add(new($"lineas[{i}].cantidad", $"La cantidad de la línea {i + 1} no es negativa."));
            if (l.PrecioUnitario < 0) errores.Add(new($"lineas[{i}].precioUnitario", $"El precio de la línea {i + 1} no es negativo."));
            var existente = l.Id is { } id ? _lineas.FirstOrDefault(x => x.Id == id) : null;
            // Un producto archivado no se ofrece en una línea nueva; la que ya lo tiene lo conserva (caso límite).
            if (!l.Producto.IsActive && (existente is null || existente.ProductId != l.Producto.Id))
                errores.Add(new($"lineas[{i}].productoId", $"{l.Producto.Etiqueta} ya no está activo en CONTPAQi."));
        }
        if (errores.Count > 0) throw new DatosIncompletosException("Las líneas no son válidas.", errores);

        _lineas.RemoveAll(x => !conservadas.Contains(x.Id));
        for (var i = 0; i < lineas.Count; i++)
        {
            var l = lineas[i];
            var existente = l.Id is { } id ? _lineas.First(x => x.Id == id) : null;
            if (existente is null) _lineas.Add(new SalesOrderLine(i + 1, l));
            else existente.Actualizar(i + 1, l);
        }
    }

    private void CopiarDomicilio() =>
        DeliveryAddressText = DeliveryAddressId is { } id ? Customer.Addresses.FirstOrDefault(a => a.Id == id)?.Texto ?? DeliveryAddressText : null;
}

/// <summary>Línea del pedido (<c>ven.sales_order_line</c>): cantidad en la unidad base del producto (D-127) y precio por esa unidad.</summary>
public sealed class SalesOrderLine : AuditableEntity, IParteDeDocumento
{
    public long SalesOrderId { get; private set; }

    public int Sequence { get; private set; }

    public long ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public decimal RequestedQty { get; private set; }

    /// <summary>Siempre la unidad base del producto; no se elige (D-127).</summary>
    public long RequestedPackagingUnitId { get; private set; }

    /// <summary>Por la unidad base, como <c>admMovimientos.CPRECIO</c> (D-74, D-146).</summary>
    public decimal? UnitPrice { get; private set; }

    /// <summary>Dato; su uso es de F2 (P-25).</summary>
    public decimal? TargetProductionKg { get; private set; }

    public decimal? TolerancePercentageOverride { get; private set; }

    public long? ForcedRouteId { get; private set; }

    public string? ErpDocumentLineId { get; private set; }

    private SalesOrderLine() { }

    internal SalesOrderLine(int sequence, LineaSolicitada l) => Actualizar(sequence, l);

    internal void Actualizar(int sequence, LineaSolicitada l)
    {
        Sequence = sequence;
        Product = l.Producto;
        ProductId = l.Producto.Id;
        RequestedPackagingUnitId = l.Producto.UnidadBase.Id;
        RequestedQty = l.Cantidad;
        UnitPrice = l.PrecioUnitario;
        TargetProductionKg = l.MetaProduccionKg;
        TolerancePercentageOverride = l.ToleranciaPorcentaje;
    }
}

/// <summary>Firma de autorización (<c>ven.authorization_signature</c>): una por rol, de personas distintas (RF-4).</summary>
public sealed class AuthorizationSignature : IParteDeDocumento
{
    public long Id { get; private set; }

    public long SalesOrderId { get; private set; }

    public RolFirma Role { get; private set; }

    public long UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string? GroupExercised { get; private set; }

    /// <summary>Si firmó como suplente (D-38).</summary>
    public bool IsSubstitute { get; private set; }

    public DateTimeOffset SignedAt { get; private set; }

    private AuthorizationSignature() { }

    internal AuthorizationSignature(RolFirma rol, Actor actor, DateTimeOffset cuando)
    {
        Role = rol;
        UserId = actor.UserId;
        UserName = actor.Nombre;
        GroupExercised = actor.Grupo;
        IsSubstitute = actor.EsSuplente;
        SignedAt = cuando;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contpaq.Bridge.Core.Contract
{
    /// <summary>
    /// Lee el sobre y la carga de un comando (§2 y §5). Lo que no cumple el esquema se rechaza aquí
    /// con 400 y no crea transacción; las reglas de negocio (CT-39) las aplica después el validador.
    /// </summary>
    public static class LectorComandos
    {
        private static readonly JsonSerializerOptions Estricto = new()
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            NumberHandling = JsonNumberHandling.Strict,
        };

        public static (ComandoLeido? Comando, ErrorContrato? Error) Leer(ComandoRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.ContractVersion))
                return Falta("contract_version");
            if (r.ContractVersion.Split('.')[0] != Contrato.VersionMayor)
                return (null, ErrorContrato.De(CodigosError.VersionNoSoportada,
                    $"La versión {r.ContractVersion} no es compatible con {Contrato.VersionActual}.",
                    new() { ["contract_version"] = r.ContractVersion }));

            if (string.IsNullOrWhiteSpace(r.CommandType) || !Comandos.Todos.Contains(r.CommandType))
                return Invalida("command_type", $"Comando desconocido: '{r.CommandType}'.");
            if (string.IsNullOrWhiteSpace(r.IdempotencyKey)) return Falta("idempotency_key");
            if (r.IdempotencyKey.Length > 150) return Invalida("idempotency_key", "Máximo 150 caracteres.");
            if (string.IsNullOrWhiteSpace(r.CorrelationId)) return Falta("correlation_id");
            if (r.ClientAppId != Contrato.ClientApp) return Invalida("client_app_id", $"Debe ser '{Contrato.ClientApp}'.");
            if (!Uri.TryCreate(r.CallbackUrl, UriKind.Absolute, out _)) return Invalida("callback_url", "Debe ser una URL absoluta.");
            if (r.Payload.ValueKind != JsonValueKind.Object) return Falta("payload");

            if (r.CommandType == Comandos.Traspaso)
            {
                if (string.IsNullOrWhiteSpace(r.Variant)) return Falta("variant");
                if (!Comandos.VariantesTraspaso.Contains(r.Variant)) return Invalida("variant", $"Variante desconocida: '{r.Variant}'.");
            }

            try
            {
                ICarga carga = r.CommandType switch
                {
                    Comandos.Traspaso => r.Payload.Deserialize<CargaTraspaso>(Estricto)!,
                    Comandos.AltaPedido => r.Payload.Deserialize<CargaAltaPedido>(Estricto)!,
                    Comandos.AltaAlmacen => r.Payload.Deserialize<CargaAltaAlmacen>(Estricto)!,
                    Comandos.CierreProduccion => r.Payload.Deserialize<CargaCierreProduccion>(Estricto)!,
                    _ => r.Payload.Deserialize<CargaRemision>(Estricto)!,
                };
                var error = Revisar(carga);
                return error is null ? (new ComandoLeido(r.CommandType, r.Variant, carga), null) : (null, error);
            }
            catch (JsonException ex)
            {
                return Invalida(ex.Path ?? "payload", ex.Message);
            }
        }

        private static ErrorContrato? Revisar(ICarga carga) => carga switch
        {
            CargaTraspaso t =>
                Fecha(t.Fecha, "fecha") ?? Requerido(t.ReferenciaNegocio, "referencia_negocio")
                ?? Requerido(t.AlmacenOrigen, "almacen_origen") ?? Requerido(t.AlmacenDestino, "almacen_destino")
                ?? Lineas(t.Lineas, "lineas", minimo: 1),
            CargaAltaPedido p =>
                Fecha(p.Fecha, "fecha") ?? Requerido(p.ReferenciaNegocio, "referencia_negocio")
                ?? Requerido(p.Cliente, "cliente") ?? Requerido(p.Moneda, "moneda")
                ?? (p.Agente is not null && string.IsNullOrWhiteSpace(p.Agente) ? ErrorDe("agente", "Si viene, no puede estar vacío.") : null)
                ?? (p.TipoCambio is null or <= 0 ? ErrorDe("tipo_cambio", "Debe ser mayor que cero.") : null)
                ?? LineasPedido(p.Lineas),
            CargaAltaAlmacen a =>
                Requerido(a.Codigo, "codigo") ?? Requerido(a.Nombre, "nombre") ?? Fecha(a.FechaAlta, "fecha_alta"),
            CargaCierreProduccion c =>
                Fecha(c.Fecha, "fecha") ?? Requerido(c.ReferenciaNegocio, "referencia_negocio")
                ?? Requerido(c.AlmacenWip, "almacen_wip") ?? Lineas(c.Consumos, "consumos", minimo: 1)
                ?? Lineas(c.Entradas, "entradas", minimo: 1) ?? Lineas(c.Subproductos, "subproductos", minimo: 0)
                ?? Destinos(c.Entradas, "entradas") ?? Destinos(c.Subproductos, "subproductos"),
            CargaRemision m =>
                Fecha(m.Fecha, "fecha") ?? Requerido(m.ReferenciaNegocio, "referencia_negocio")
                ?? Requerido(m.Cliente, "cliente") ?? Requerido(m.Almacen, "almacen")
                ?? (m.CierraPedido is null ? ErrorDe("cierra_pedido", "Es obligatorio.") : null)
                ?? Lineas(m.Lineas, "lineas", minimo: 1)
                ?? m.Lineas!.Select((l, i) => l.Precio < 0 ? ErrorDe($"lineas[{i}].precio", "No puede ser negativo.") : null).FirstOrDefault(e => e is not null),
            _ => ErrorDe("payload", "Carga desconocida."),
        };

        private static ErrorContrato? Lineas<T>(List<T>? lineas, string campo, int minimo) where T : LineaConLotes
        {
            if (lineas is null) return ErrorDe(campo, "Es obligatorio.");
            if (lineas.Count < minimo) return ErrorDe(campo, $"Debe tener al menos {minimo} línea(s).");
            for (var i = 0; i < lineas.Count; i++)
            {
                var l = lineas[i];
                var c = $"{campo}[{i}]";
                var e = Requerido(l.Producto, c + ".producto") ?? Requerido(l.Unidad, c + ".unidad")
                    ?? (l.Cantidad <= 0 ? ErrorDe(c + ".cantidad", "Debe ser mayor que cero.") : null)
                    ?? (l.Lotes is null ? ErrorDe(c + ".lotes", "Es obligatorio; vacío si el producto no lleva lote.") : null);
                if (e is not null) return e;
                for (var j = 0; j < l.Lotes!.Count; j++)
                {
                    var lote = l.Lotes[j];
                    var cl = $"{c}.lotes[{j}]";
                    e = Requerido(lote.Numero, cl + ".numero") ?? (lote.Cantidad <= 0 ? ErrorDe(cl + ".cantidad", "Debe ser mayor que cero.") : null);
                    if (e is not null) return e;
                }
            }
            return null;
        }

        private static ErrorContrato? Destinos(List<LineaConDestino>? lineas, string campo) =>
            lineas?.Select((l, i) => Requerido(l.AlmacenDestino, $"{campo}[{i}].almacen_destino")).FirstOrDefault(e => e is not null);

        private static ErrorContrato? LineasPedido(List<LineaPedido>? lineas)
        {
            if (lineas is null || lineas.Count == 0) return ErrorDe("lineas", "Debe tener al menos 1 línea.");
            for (var i = 0; i < lineas.Count; i++)
            {
                var l = lineas[i];
                var c = $"lineas[{i}]";
                var e = Requerido(l.Producto, c + ".producto") ?? Requerido(l.Unidad, c + ".unidad")
                    ?? (l.Cantidad <= 0 ? ErrorDe(c + ".cantidad", "Debe ser mayor que cero.") : null)
                    ?? (l.Precio is null or < 0 ? ErrorDe(c + ".precio", "Es obligatorio y no puede ser negativo.") : null);
                if (e is not null) return e;
            }
            return null;
        }

        private static ErrorContrato? Fecha(string? valor, string campo) =>
            DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                ? null
                : ErrorDe(campo, "Debe ser una fecha AAAA-MM-DD.");

        private static ErrorContrato? Requerido(string? valor, string campo) =>
            string.IsNullOrWhiteSpace(valor) ? ErrorDe(campo, "Es obligatorio.") : null;

        private static ErrorContrato ErrorDe(string campo, string mensaje) =>
            ErrorContrato.De(CodigosError.CargaInvalida, $"{campo}: {mensaje}", new() { ["campo"] = campo });

        private static (ComandoLeido?, ErrorContrato?) Falta(string campo) => (null, ErrorDe(campo, "Es obligatorio."));

        private static (ComandoLeido?, ErrorContrato?) Invalida(string campo, string mensaje) => (null, ErrorDe(campo, mensaje));
    }
}

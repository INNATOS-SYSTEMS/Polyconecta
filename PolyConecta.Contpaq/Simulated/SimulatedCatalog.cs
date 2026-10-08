using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contpaq.Bridge.Core.Contract;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>
    /// Catálogo del modo simulado, leído de BridgeConfig__Simulated__SeedPath. Productos y clientes
    /// se pueden cambiar en caliente (C-T006); los cambios viven en memoria y se pierden al reiniciar.
    /// </summary>
    public sealed class SimulatedCatalog
    {
        private readonly object _candado = new();

        [JsonPropertyName("productos")] public List<ProductoContrato> Productos { get; set; } = new();
        [JsonPropertyName("clientes")] public List<ClienteContrato> Clientes { get; set; } = new();
        [JsonPropertyName("agentes")] public List<AgenteContrato> Agentes { get; set; } = new();
        [JsonPropertyName("almacenes")] public List<AlmacenContrato> Almacenes { get; set; } = new();
        [JsonPropertyName("existencias")] public List<ExistenciaSemilla> Existencias { get; set; } = new();
        [JsonPropertyName("recepciones_compra")] public List<RecepcionCompraContrato> RecepcionesCompra { get; set; } = new();

        public static SimulatedCatalog Cargar(string ruta) =>
            JsonSerializer.Deserialize<SimulatedCatalog>(File.ReadAllText(ruta))
            ?? throw new InvalidDataException($"No se pudo leer el catálogo simulado {ruta}.");

        /// <summary>Copia de los productos en este momento, segura frente a cambios en caliente.</summary>
        public IReadOnlyList<ProductoContrato> ProductosActuales() { lock (_candado) return Productos.ToList(); }

        public IReadOnlyList<ClienteContrato> ClientesActuales() { lock (_candado) return Clientes.ToList(); }

        /// <summary>Agrega o reemplaza el producto de ese código. Si no trae id_erp, conserva el que tenía o toma el siguiente.</summary>
        public ProductoContrato GuardarProducto(string codigo, ProductoContrato nuevo)
        {
            lock (_candado)
            {
                var previo = Productos.FirstOrDefault(p => p.Codigo == codigo);
                nuevo.Codigo = codigo;
                nuevo.IdErp = nuevo.IdErp != 0 ? nuevo.IdErp : previo?.IdErp ?? Productos.Select(p => p.IdErp).DefaultIfEmpty(0).Max() + 1;
                if (previo is not null) Productos.Remove(previo);
                Productos.Add(nuevo);
                return nuevo;
            }
        }

        public bool QuitarProducto(string codigo)
        {
            lock (_candado) return Productos.RemoveAll(p => p.Codigo == codigo) > 0;
        }

        /// <summary>Agrega o reemplaza el cliente de ese código; el id_erp se resuelve como en los productos.</summary>
        public ClienteContrato GuardarCliente(string codigo, ClienteContrato nuevo)
        {
            lock (_candado)
            {
                var previo = Clientes.FirstOrDefault(c => c.Codigo == codigo);
                nuevo.Codigo = codigo;
                nuevo.IdErp = nuevo.IdErp != 0 ? nuevo.IdErp : previo?.IdErp ?? Clientes.Select(c => c.IdErp).DefaultIfEmpty(0).Max() + 1;
                if (previo is not null) Clientes.Remove(previo);
                Clientes.Add(nuevo);
                return nuevo;
            }
        }

        public bool QuitarCliente(string codigo)
        {
            lock (_candado) return Clientes.RemoveAll(c => c.Codigo == codigo) > 0;
        }
    }

    public sealed class ExistenciaSemilla
    {
        [JsonPropertyName("producto")] public string Producto { get; set; } = string.Empty;
        [JsonPropertyName("almacen")] public string Almacen { get; set; } = string.Empty;
        [JsonPropertyName("lote")] public string? Lote { get; set; }
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
    }
}

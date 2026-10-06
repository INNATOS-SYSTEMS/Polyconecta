using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contpaq.Bridge.Core.Contract;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>Catálogo del modo simulado, leído de BridgeConfig__Simulated__SeedPath.</summary>
    public sealed class SimulatedCatalog
    {
        [JsonPropertyName("productos")] public List<ProductoContrato> Productos { get; set; } = new();
        [JsonPropertyName("clientes")] public List<ClienteContrato> Clientes { get; set; } = new();
        [JsonPropertyName("almacenes")] public List<AlmacenContrato> Almacenes { get; set; } = new();
        [JsonPropertyName("existencias")] public List<ExistenciaSemilla> Existencias { get; set; } = new();
        [JsonPropertyName("recepciones_compra")] public List<RecepcionCompraContrato> RecepcionesCompra { get; set; } = new();

        public static SimulatedCatalog Cargar(string ruta) =>
            JsonSerializer.Deserialize<SimulatedCatalog>(File.ReadAllText(ruta))
            ?? throw new InvalidDataException($"No se pudo leer el catálogo simulado {ruta}.");
    }

    public sealed class ExistenciaSemilla
    {
        [JsonPropertyName("producto")] public string Producto { get; set; } = string.Empty;
        [JsonPropertyName("almacen")] public string Almacen { get; set; } = string.Empty;
        [JsonPropertyName("lote")] public string? Lote { get; set; }
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
    }
}

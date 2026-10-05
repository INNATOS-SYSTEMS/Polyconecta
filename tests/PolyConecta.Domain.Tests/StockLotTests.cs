using AwesomeAssertions;
using PolyConecta.Domain.Entities;
using PolyConecta.Domain.ValueObjects;
using Xunit;

namespace PolyConecta.Domain.Tests;

public class StockLotTests
{
    [Fact]
    public void Folio_Generate_ShouldMatchRegexPattern()
    {
        var timestamp = new DateTime(2026, 9, 10, 4, 27, 47);
        var folio = Folio.Generate(1, timestamp);

        folio.Value.Should().Be("EX-01-260910-042747");
    }

    [Fact]
    public void StockLot_NetWeight_ShouldEqualGrossMinusTare()
    {
        var lot = new StockLot
        {
            GrossWeightKg = 155.400m,
            TareWeightKg = 5.400m
        };

        lot.NetWeightKg.Should().Be(150.000m);
    }

    [Fact]
    public void StockLot_ShouldLinkToTheManufacturingOrderThatProducedIt()
    {
        var order = new ManufacturingOrder { Name = "OF-EXT-2026-0001-1", ProcessType = "Extrusion" };
        var lot = new StockLot { Name = "EX-01-260910-042747", ManufacturingOrderId = order.Id };

        lot.ManufacturingOrderId.Should().Be(order.Id);
    }
}

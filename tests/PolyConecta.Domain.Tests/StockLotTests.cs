using AwesomeAssertions;
using PolyConecta.Domain.Entities;
using Xunit;

namespace PolyConecta.Domain.Tests;

public class StockLotTests
{
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

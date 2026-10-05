using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Api.Controllers;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.IntegrationTests.Infraestructura;
using Xunit;

namespace PolyConecta.IntegrationTests;

public class OrdersApiTests(SqlServerFixture sql)
{
    private Task<PolyDbContext> GetDbContext() => sql.CrearContextoAsync();

    private static readonly string[] ProcesosEsperados = { "Extrusion", "Printing", "Bagging" };

    [Fact]
    public async Task CreateMasterOrder_ShouldAutoDecomposeProcessOrders()
    {
        await using var db = await GetDbContext();
        var controller = new OrdersController(db);

        var request = new OrdersController.CreateMasterOrderRequest(421, "CLI-100", "PT-BAG-001", 5000.0m);
        var result = await controller.CreateMasterOrder(request, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>();

        var maestras = await db.ManufacturingOrders
            .Include(x => x.ChildOrders)
            .Where(x => x.ProcessType == "Master")
            .ToListAsync();

        maestras.Should().HaveCount(1);
        maestras[0].ChildOrders.Should().HaveCount(3);
        maestras[0].ChildOrders.Select(s => s.ProcessType).Should().Contain(ProcesosEsperados);
        maestras[0].ChildOrders.All(s => s.State == "Draft").Should().BeTrue();
    }

    [Fact]
    public async Task CreateMasterOrder_ShouldNotLeaveLegacyDuplicates()
    {
        await using var db = await GetDbContext();
        var controller = new OrdersController(db);

        await controller.CreateMasterOrder(
            new OrdersController.CreateMasterOrderRequest(421, "CLI-100", "PT-BAG-001", 5000.0m),
            CancellationToken.None);

        // La jerarquía vive en una sola tabla autoreferenciada: 1 maestra + 3 hijas.
        var total = await db.ManufacturingOrders.CountAsync();
        total.Should().Be(4);
    }
}

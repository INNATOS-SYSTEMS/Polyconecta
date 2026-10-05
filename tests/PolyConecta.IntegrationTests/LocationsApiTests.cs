using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Api.Controllers;
using PolyConecta.Domain.Entities;
using PolyConecta.Infrastructure.Outbox;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.IntegrationTests.Infraestructura;
using Xunit;

namespace PolyConecta.IntegrationTests;

public class LocationsApiTests(SqlServerFixture sql)
{
    // Los almacenes PIM/Cuarentena y PIM/Stock/PT vienen en la semilla de la migración F0_Base.
    private Task<PolyDbContext> GetDbContext() => sql.CrearContextoAsync();

    [Fact]
    public async Task Transfer_FromQuarantine_ShouldReturnUnprocessableEntity_HardStopGate()
    {
        await using var db = await GetDbContext();
        var outbox = new OutboxPublisher();
        var controller = new LocationsController(db, outbox);

        var request = new LocationsController.StockTransferRequest("EX-01-260910-042747", "PIM/Cuarentena", "PIM/Stock/PT", "OP-01");
        var result = await controller.ExecuteStockTransfer(request, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult>();
    }
}

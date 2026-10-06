using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class LocationsController : ControllerBase
{
    private readonly PolyDbContext _db;

    public LocationsController(PolyDbContext db)
    {
        _db = db;
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations(CancellationToken cancellationToken)
    {
        var locations = await _db.Locations.Where(l => l.IsActive).ToListAsync(cancellationToken);
        return Ok(locations);
    }

    public record StockTransferRequest(string RollFolio, string SourceLocationCode, string TargetLocationCode, string? OperatorId);

    [HttpPost("transfers/move")]
    public async Task<IActionResult> ExecuteStockTransfer([FromBody] StockTransferRequest request, CancellationToken cancellationToken)
    {
        if (request.SourceLocationCode.Contains("Cuarentena", StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(new
            {
                Error = "Quality Gate Hard-Stop: Material in Quarantine cannot be moved to Finished Goods stock without a verified Digital Quality Release."
            });
        }

        var sourceLoc = await _db.Locations.FirstOrDefaultAsync(l => l.LocationCode == request.SourceLocationCode, cancellationToken);
        var targetLoc = await _db.Locations.FirstOrDefaultAsync(l => l.LocationCode == request.TargetLocationCode, cancellationToken);

        if (sourceLoc == null || targetLoc == null)
        {
            return BadRequest(new { Error = "Invalid source or target location code." });
        }

        var transferId = Guid.NewGuid();


        return Ok(new
        {
            TransferId = transferId,
            Status = "Success"
        });
    }
}

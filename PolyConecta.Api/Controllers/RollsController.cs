using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Entities;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rolls")]
public class RollsController : ControllerBase
{
    private readonly PolyDbContext _db;
    private readonly IReferenceSequenceService _folios;

    public RollsController(PolyDbContext db, IReferenceSequenceService folios)
    {
        _db = db;
        _folios = folios;
    }

    public record CaptureRollRequest(
        Guid ManufacturingOrderId,
        int LineId,
        string ProductSku,
        decimal GrossWeightKg,
        decimal TareWeightKg,
        decimal LengthMeters,
        decimal GaugeMicron,
        decimal WidthMm,
        decimal DynesCm,
        string MachineId,
        string Shift,
        string OperatorId
    );

    [HttpPost("capture")]
    public async Task<IActionResult> CaptureRoll([FromBody] CaptureRollRequest request, CancellationToken cancellationToken)
    {
        if (request.GrossWeightKg <= request.TareWeightKg)
        {
            return BadRequest(new { Error = "Gross weight must be greater than tare weight." });
        }

        var folio = await _folios.NextAsync(
            "ROLLO_EXTRUSION",
            new Dictionary<string, string> { ["linea"] = request.LineId.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) },
            cancellationToken);
        var roll = new StockLot
        {
            ManufacturingOrderId = request.ManufacturingOrderId,
            Name = folio,
            ContpaqLotNumber = folio, // 1:1 mapping to CONTPAQi cNumeroLote
            ProductSku = request.ProductSku,
            GrossWeightKg = request.GrossWeightKg,
            TareWeightKg = request.TareWeightKg,
            LengthMeters = request.LengthMeters,
            GaugeMicron = request.GaugeMicron,
            WidthMm = request.WidthMm,
            DynesCm = request.DynesCm,
            MachineId = request.MachineId,
            Shift = request.Shift,
            OperatorId = request.OperatorId,
            Status = "Available",
            CurrentLocationCode = "PIM/Produccion"
        };

        _db.StockLots.Add(roll);
        await _db.SaveChangesAsync(cancellationToken);


        return CreatedAtAction(nameof(GetRollByFolio), new { folio = roll.Name }, roll);
    }

    [HttpGet("{folio}")]
    public async Task<IActionResult> GetRollByFolio(string folio, CancellationToken cancellationToken)
    {
        var roll = await _db.StockLots.FirstOrDefaultAsync(r => r.Name == folio, cancellationToken);
        if (roll == null) return NotFound();
        return Ok(roll);
    }
}

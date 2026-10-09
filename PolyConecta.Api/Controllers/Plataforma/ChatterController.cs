using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Chatter;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>Historial del chatter de un documento (contracts/api-f1.md, Chatter); los mensajes nuevos van por el hub.</summary>
[ApiController]
[Route("api/v1/plataforma/chatter")]
public sealed class ChatterController(IUseCase<ObtenerChatter, IReadOnlyList<MensajeChatterDto>> obtener) : ControllerBase
{
    [HttpGet("{tipo}/{id:long}")]
    public Task<IReadOnlyList<MensajeChatterDto>> Historial(string tipo, long id, [FromQuery] long? antesDe, CancellationToken ct) =>
        obtener.ExecuteAsync(new ObtenerChatter(tipo, id, antesDe), ct);
}

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Contpaq.Bridge.Api.Controllers
{
    [ApiController]
    [Route("api/v1/dlq")]
    public class DlqController : ControllerBase
    {
        private readonly IOutboxRepository _outboxRepository;

        public DlqController(IOutboxRepository outboxRepository)
        {
            _outboxRepository = outboxRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetDlqItems()
        {
            var items = await _outboxRepository.GetDlqTransactionsAsync();
            return Ok(items);
        }

        [HttpPost("{id}/retry")]
        public async Task<IActionResult> RetryDlqItem(string id)
        {
            if (!await _outboxRepository.ReencolarAsync(id, null))
            {
                return NotFound(new { error = "Item not found in DLQ" });
            }
            return Ok(new { message = $"Transaction {id} re-queued for execution" });
        }

        public class EditPayloadRequest
        {
            public JsonElement Payload { get; set; }
        }

        /// <summary>Reencola con la carga corregida. La carga nueva sí se guarda (antes se perdía).</summary>
        [HttpPost("{id}/edit-and-retry")]
        public async Task<IActionResult> EditAndRetryDlqItem(string id, [FromBody] EditPayloadRequest request)
        {
            if (!await _outboxRepository.ReencolarAsync(id, request.Payload.GetRawText()))
            {
                return NotFound(new { error = "Item not found in DLQ" });
            }
            return Ok(new { message = $"Transaction {id} updated and re-queued" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> PurgeDlqItem(string id)
        {
            var success = await _outboxRepository.DeleteDlqTransactionAsync(id);
            if (!success)
            {
                return NotFound(new { error = "Item not found in DLQ" });
            }
            return Ok(new { message = $"Transaction {id} purged from DLQ" });
        }
    }
}

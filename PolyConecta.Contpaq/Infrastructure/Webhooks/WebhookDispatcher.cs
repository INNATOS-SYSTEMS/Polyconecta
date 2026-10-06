using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Contpaq.Bridge.Infrastructure.Webhooks
{
    public interface IWebhookDispatcher
    {
        /// <summary>Envía el callback firmado y reintenta con espera creciente si no recibe 2xx (§3).</summary>
        Task DeliverAsync(BridgeTransaction transaction, CancellationToken cancellationToken = default);
    }

    public sealed partial class WebhookDispatcher(
        IHttpClientFactory httpClients, IOutboxRepository repo, BridgeOptions opciones, ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
    {
        public const string CabeceraFirma = "X-Bridge-Signature";

        /// <summary>Esperas entre intentos: 1, 2, 4 y 8 segundos (5 intentos en total).</summary>
        private static readonly TimeSpan[] Esperas = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)];

        public async Task DeliverAsync(BridgeTransaction transaction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transaction.CallbackUrl)) return;
            var cuerpo = CuerpoCallback.Serializar(transaction);
            var cliente = httpClients.CreateClient(nameof(WebhookDispatcher));

            for (var intento = 1; intento <= Esperas.Length + 1; intento++)
            {
                int? status = null;
                string? respuesta = null;
                try
                {
                    using var peticion = new HttpRequestMessage(HttpMethod.Post, transaction.CallbackUrl)
                    {
                        Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
                    };
                    peticion.Headers.TryAddWithoutValidation(CabeceraFirma, Firmar(cuerpo, opciones.CallbackSecret, DateTimeOffset.UtcNow));
                    peticion.Headers.TryAddWithoutValidation("X-Correlation-ID", transaction.CorrelationId);
                    using var r = await cliente.SendAsync(peticion, cancellationToken);
                    status = (int)r.StatusCode;
                    respuesta = await r.Content.ReadAsStringAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    respuesta = ex.Message;
                }

                await repo.RegistrarEntregaCallbackAsync(transaction.TransactionId, transaction.CallbackUrl, status, Recortar(respuesta), intento);
                if (status is >= 200 and < 300) return;
                if (intento <= Esperas.Length)
                    await Task.Delay(Esperas[intento - 1], cancellationToken);
            }
            LogCallbackPerdido(logger, transaction.TransactionId, transaction.CorrelationId);
        }

        /// <summary>t={unix},v1={HMAC-SHA256(secreto, t + "." + cuerpo)} en hexadecimal (D-121, §3).</summary>
        public static string Firmar(string cuerpo, string secreto, DateTimeOffset cuando)
        {
            var t = cuando.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(t + "." + cuerpo));
            return $"t={t},v1={Convert.ToHexStringLower(hash)}";
        }

        private static string? Recortar(string? texto) => texto is { Length: > 500 } ? texto[..500] : texto;

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "No se pudo entregar el callback de {TransactionId} [{CorrelationId}]; PolyConecta puede consultarlo con GET /api/v1/transactions/{{id}}")]
        private static partial void LogCallbackPerdido(ILogger logger, string transactionId, string correlationId);
    }
}

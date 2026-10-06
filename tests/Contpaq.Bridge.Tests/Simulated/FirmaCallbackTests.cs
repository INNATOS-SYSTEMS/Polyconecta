using System;
using AwesomeAssertions;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Xunit;

namespace Contpaq.Bridge.Tests.Simulated
{
    /// <summary>Firma del callback (D-121, contrato §3).</summary>
    public class FirmaCallbackTests
    {
        [Fact]
        public void Firma_t_punto_cuerpo_con_HMAC_SHA256_en_hexadecimal()
        {
            var cuando = DateTimeOffset.FromUnixTimeSeconds(1760000000);
            // Vector calculado aparte: HMAC-SHA256("secreto", "1760000000.{\"a\":1}").
            var esperado = Convert.ToHexStringLower(System.Security.Cryptography.HMACSHA256.HashData(
                "secreto"u8.ToArray(), "1760000000.{\"a\":1}"u8.ToArray()));

            WebhookDispatcher.Firmar("{\"a\":1}", "secreto", cuando).Should().Be($"t=1760000000,v1={esperado}");
        }

        [Fact]
        public void Otro_secreto_u_otro_cuerpo_dan_otra_firma()
        {
            var cuando = DateTimeOffset.FromUnixTimeSeconds(1760000000);
            var firma = WebhookDispatcher.Firmar("{}", "secreto", cuando);
            WebhookDispatcher.Firmar("{}", "otro", cuando).Should().NotBe(firma);
            WebhookDispatcher.Firmar("{ }", "secreto", cuando).Should().NotBe(firma);
        }
    }
}

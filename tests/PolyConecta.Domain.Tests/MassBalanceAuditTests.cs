using AwesomeAssertions;
using PolyConecta.Domain.Services;
using Xunit;

namespace PolyConecta.Domain.Tests;

public class MassBalanceAuditTests
{
    [Fact]
    public void AuditSubOrderRun_WithinTolerance_ShouldPassAudit()
    {
        var subOrderId = Guid.NewGuid();
        var audit = MassBalanceService.AuditSubOrderRun(subOrderId, mpInputKg: 1000.0m, rollOutputKg: 970.0m, scrapOutputKg: 20.0m);

        audit.AuditPassed.Should().BeTrue();
        audit.VariancePercentage.Should().Be(1.0m); // (1000 - 990)/1000 = 1%
        audit.FlagReason.Should().BeNull();
    }

    [Fact]
    public void AuditSubOrderRun_ExceedingTolerance_ShouldFailAuditAndSetFlagReason()
    {
        var subOrderId = Guid.NewGuid();
        var audit = MassBalanceService.AuditSubOrderRun(subOrderId, mpInputKg: 1000.0m, rollOutputKg: 940.0m, scrapOutputKg: 20.0m);

        audit.AuditPassed.Should().BeFalse();
        audit.VariancePercentage.Should().Be(4.0m); // (1000 - 960)/1000 = 4%
        audit.FlagReason.Should().Contain("exceeds allowed tolerance");
    }
}

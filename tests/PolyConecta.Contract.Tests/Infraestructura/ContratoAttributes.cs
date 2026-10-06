using Xunit;

namespace PolyConecta.Contract.Tests.Infraestructura;

/// <summary>
/// La suite solo corre cuando hay un bridge al cual apuntar: se activa definiendo BRIDGE_URL. Así
/// `dotnet test Polyconecta.slnx` no falla en una máquina sin bridge, y la CI la corre en su
/// trabajo `contrato` con el simulador levantado.
/// </summary>
public static class Activacion
{
    public static bool HayBridge => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BRIDGE_URL"));

    public const string Motivo = "Suite de contrato: define BRIDGE_URL para correrla contra un bridge (ver README).";
}

public sealed class ContratoFactAttribute : FactAttribute
{
    public ContratoFactAttribute()
    {
        Skip = Activacion.Motivo;
        SkipType = typeof(Activacion);
        SkipUnless = nameof(Activacion.HayBridge);
    }
}

public sealed class ContratoTheoryAttribute : TheoryAttribute
{
    public ContratoTheoryAttribute()
    {
        Skip = Activacion.Motivo;
        SkipType = typeof(Activacion);
        SkipUnless = nameof(Activacion.HayBridge);
    }
}

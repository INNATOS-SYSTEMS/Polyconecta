using Xunit;

namespace PolyConecta.Domain.Tests;

/// <summary>
/// Prueba rota a propósito para verificar que la regla de main bloquea un PR en rojo (SC-007, L2-T025).
/// Vive solo en la rama prueba-proteccion-main, que no se integra.
/// </summary>
public class ProteccionDeMainTests
{
    [Fact]
    public void Rota_a_proposito_para_probar_la_proteccion_de_main() =>
        Assert.Fail("Prueba rota a propósito: el PR debe quedar en rojo y no se debe poder integrar.");
}

using System.Collections.Generic;
using Contpaq.Bridge.Infrastructure.Outbox;

namespace Contpaq.Bridge.Tests.Soporte
{
    /// <summary>Anota los códigos con los que el worker pide salir del proceso, sin salir.</summary>
    public sealed class CierreProcesoFalso : ICierreProceso
    {
        public List<int> Codigos { get; } = new();

        public void Salir(int codigo) => Codigos.Add(codigo);
    }
}

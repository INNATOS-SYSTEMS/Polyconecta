using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Infrastructure.Plataforma.Listas;

/// <summary>
/// Navegaciones necesarias únicamente para proyectar la lista o evaluar agrupaciones (evita SplitQuery y carga de hijos innecesarios).
/// </summary>
public interface IIncluirEnLista<T> where T : class
{
    IQueryable<T> Incluir(IQueryable<T> consulta);
}

/// <summary>La lista de pedidos solo requiere la navegación a su cliente.</summary>
public sealed class IncluirPedidoEnLista : IIncluirEnLista<SalesOrder>
{
    public IQueryable<SalesOrder> Incluir(IQueryable<SalesOrder> consulta) =>
        consulta.Include(o => o.Customer);
}

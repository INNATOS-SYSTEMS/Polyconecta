using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
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

/// <summary>La lista de productos muestra el nombre de su clasificación y agrupa por él.</summary>
public sealed class IncluirProductoEnLista : IIncluirEnLista<Product>
{
    public IQueryable<Product> Incluir(IQueryable<Product> consulta) => consulta.Include(p => p.Classification);
}

/// <summary>La lista de usuarios muestra sus grupos y plantas.</summary>
public sealed class IncluirUsuarioEnLista : IIncluirEnLista<User>
{
    public IQueryable<User> Incluir(IQueryable<User> consulta) =>
        consulta.Include(u => u.Assignments).ThenInclude(a => a.Group).Include(u => u.Assignments).ThenInclude(a => a.Plant);
}

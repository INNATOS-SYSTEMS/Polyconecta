using System.Linq.Expressions;

namespace PolyConecta.Application.Common;

/// <summary>Combina predicados sobre el mismo tipo en uno solo que EF Core puede traducir.</summary>
public static class Expresiones
{
    public static Expression<Func<T, bool>> Y<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b) => Combinar(a, b, Expression.AndAlso);

    public static Expression<Func<T, bool>> O<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b) => Combinar(a, b, Expression.OrElse);

    public static Expression<Func<T, bool>> Todos<T>(IEnumerable<Expression<Func<T, bool>>> predicados) =>
        predicados.Aggregate((Expression<Func<T, bool>>)(_ => true), Y);

    private static Expression<Func<T, bool>> Combinar<T>(
        Expression<Func<T, bool>> a, Expression<Func<T, bool>> b, Func<Expression, Expression, BinaryExpression> op)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var p = a.Parameters[0];
        var cuerpoB = new Reemplazo(b.Parameters[0], p).Visit(b.Body);
        return Expression.Lambda<Func<T, bool>>(op(a.Body, cuerpoB), p);
    }

    /// <summary>Sustituye un parámetro por otra expresión (el cuerpo de una proyección, por ejemplo).</summary>
    public sealed class Reemplazo(ParameterExpression desde, Expression hacia) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == desde ? hacia : base.VisitParameter(node);
    }
}

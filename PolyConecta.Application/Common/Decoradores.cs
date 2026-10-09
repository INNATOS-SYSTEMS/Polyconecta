using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PolyConecta.Application.Common;

/// <summary>Registra inicio, fin, duración y correlation_id de cada caso de uso (CT-31).</summary>
public sealed partial class LoggingDecorator<TRequest, TResult>(
    IUseCase<TRequest, TResult> inner,
    ICorrelationContext correlation,
    ILogger<LoggingDecorator<TRequest, TResult>> logger) : IUseCase<TRequest, TResult>
{
    public async Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var nombre = inner.GetType().Name;
        var reloj = Stopwatch.StartNew();
        try
        {
            var resultado = await inner.ExecuteAsync(request, cancellationToken);
            LogTermino(logger, nombre, correlation.CorrelationId, reloj.ElapsedMilliseconds);
            return resultado;
        }
        catch (Exception ex)
        {
            LogFallo(logger, ex, nombre, correlation.CorrelationId, reloj.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Caso de uso {UseCase} terminó [{CorrelationId}] en {Ms} ms")]
    private static partial void LogTermino(ILogger logger, string useCase, string correlationId, long ms);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Caso de uso {UseCase} falló [{CorrelationId}] en {Ms} ms")]
    private static partial void LogFallo(ILogger logger, Exception ex, string useCase, string correlationId, long ms);
}

/// <summary>
/// Abre la transacción, ejecuta, guarda y confirma. El outbox se escribe en el mismo DbContext,
/// así que si falla el negocio tampoco queda el mensaje (CT-20). Un caso de uso anidado se une a
/// la transacción que ya está abierta.
/// </summary>
public sealed class TransactionDecorator<TRequest, TResult>(
    IUseCase<TRequest, TResult> inner,
    IUnitOfWork unitOfWork,
    Plataforma.Chatter.IChatterNotificador? chatter = null) : IUseCase<TRequest, TResult>
{
    public async Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (unitOfWork.HasActiveTransaction)
            return await inner.ExecuteAsync(request, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        TResult resultado;
        try
        {
            resultado = await inner.ExecuteAsync(request, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            chatter?.Descartar();
            throw;
        }
        // El chatter se transmite solo con la transacción confirmada (R-04).
        if (chatter is not null) await chatter.EnviarAsync(CancellationToken.None);
        return resultado;
    }
}

/// <summary>Corre el validador de la petición, si existe, antes de tocar la base.</summary>
public sealed class ValidationDecorator<TRequest, TResult>(
    IUseCase<TRequest, TResult> inner,
    IEnumerable<IValidator<TRequest>> validators) : IUseCase<TRequest, TResult>
{
    public Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errores = validators.SelectMany(v => v.Validate(request)).ToList();
        if (errores.Count > 0)
            throw new ValidacionException(errores);
        return inner.ExecuteAsync(request, cancellationToken);
    }
}

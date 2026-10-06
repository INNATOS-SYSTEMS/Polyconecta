using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Contpaq.Bridge.Infrastructure.Persistence
{
    public interface IOutboxRepository
    {
        /// <summary>False si ya existe la idempotency_key (CT-19).</summary>
        Task<bool> AddTransactionAsync(BridgeTransaction transaction);
        Task<BridgeTransaction?> GetByIdAsync(string transactionId);
        Task<BridgeTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey);
        /// <summary>Pendientes listas para procesarse, en orden de llegada (CT-41).</summary>
        Task<IEnumerable<BridgeTransaction>> GetPendingTransactionsAsync(int limit = 20);
        Task MarcarProcesandoAsync(string transactionId);
        /// <summary>Estado terminal: CONFIRMED con su resultado, o FAILED / DEAD_LETTER con su error.</summary>
        Task CompletarAsync(string transactionId, string estado, string? resultJson, string? errorJson, string? mensaje);
        /// <summary>Error reintentable (§4): vuelve a PENDING para el siguiente intento.</summary>
        Task ProgramarReintentoAsync(string transactionId, int retryCount, DateTime nextAttemptAt, string errorJson, string mensaje);
        /// <summary>Sistemas reencola una transacción de la DLQ, opcionalmente con la carga corregida.</summary>
        Task<bool> ReencolarAsync(string transactionId, string? nuevaCarga);
        Task RegistrarEntregaCallbackAsync(string transactionId, string callbackUrl, int? httpStatus, string? respuesta, int intento);
        Task AddLogAsync(TransactionLog log);
        Task<IEnumerable<BridgeTransaction>> GetDlqTransactionsAsync();
        Task<bool> DeleteDlqTransactionAsync(string transactionId);
        Task<int> GetQueueDepthAsync();
        Task<int> DeletePendingTransactionsAsync();
        Task<int> PurgeAllTransactionsAsync();
        Task<bool> DeleteTransactionAsync(string transactionId);
    }

    public class OutboxRepository : IOutboxRepository
    {
        private readonly string _connectionString;

        private const string SelectFields = @"
            transaction_id AS TransactionId,
            correlation_id AS CorrelationId,
            client_app_id AS ClientAppId,
            idempotency_key AS IdempotencyKey,
            command_type AS CommandType,
            contract_version AS ContractVersion,
            variant AS Variant,
            payload_json AS PayloadJson,
            callback_url AS CallbackUrl,
            status AS Status,
            retry_count AS RetryCount,
            max_retries AS MaxRetries,
            next_attempt_at AS NextAttemptAt,
            contpaqi_doc_id AS ContpaqiDocId,
            contpaqi_folio AS ContpaqiFolio,
            last_error_code AS LastErrorCode,
            last_error_message AS LastErrorMessage,
            result_json AS ResultJson,
            error_json AS ErrorJson,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt";

        public OutboxRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private static string Ahora() => DateTime.UtcNow.ToString("o");

        public async Task<bool> AddTransactionAsync(BridgeTransaction transaction)
        {
            using var conn = GetConnection();
            const string sql = @"
                INSERT INTO bridge_transactions (
                    transaction_id, correlation_id, client_app_id, idempotency_key,
                    command_type, contract_version, variant, payload_json, callback_url, status, retry_count,
                    max_retries, next_attempt_at, created_at, updated_at
                ) VALUES (
                    @TransactionId, @CorrelationId, @ClientAppId, @IdempotencyKey,
                    @CommandType, @ContractVersion, @Variant, @PayloadJson, @CallbackUrl, @Status, @RetryCount,
                    @MaxRetries, @NextAttemptAt, @CreatedAt, @UpdatedAt
                );";
            try
            {
                return await conn.ExecuteAsync(sql, transaction) > 0;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                return false;
            }
        }

        public async Task<BridgeTransaction?> GetByIdAsync(string transactionId)
        {
            using var conn = GetConnection();
            return await conn.QuerySingleOrDefaultAsync<BridgeTransaction>(
                $"SELECT {SelectFields} FROM bridge_transactions WHERE transaction_id = @TransactionId;", new { TransactionId = transactionId });
        }

        public async Task<BridgeTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            using var conn = GetConnection();
            return await conn.QuerySingleOrDefaultAsync<BridgeTransaction>(
                $"SELECT {SelectFields} FROM bridge_transactions WHERE idempotency_key = @IdempotencyKey;", new { IdempotencyKey = idempotencyKey });
        }

        public async Task<IEnumerable<BridgeTransaction>> GetPendingTransactionsAsync(int limit = 20)
        {
            using var conn = GetConnection();
            var sql = $@"
                SELECT {SelectFields} FROM bridge_transactions
                WHERE status = @Pending AND next_attempt_at <= @Now
                ORDER BY created_at ASC, rowid ASC LIMIT @Limit;";
            return await conn.QueryAsync<BridgeTransaction>(sql, new { Pending = Estados.Pending, Now = Ahora(), Limit = limit });
        }

        public async Task MarcarProcesandoAsync(string transactionId)
        {
            using var conn = GetConnection();
            await conn.ExecuteAsync(
                "UPDATE bridge_transactions SET status = @Estado, updated_at = @Now WHERE transaction_id = @Id;",
                new { Estado = Estados.Processing, Now = Ahora(), Id = transactionId });
        }

        public async Task CompletarAsync(string transactionId, string estado, string? resultJson, string? errorJson, string? mensaje)
        {
            using var conn = GetConnection();
            const string sql = @"
                UPDATE bridge_transactions
                SET status = @Estado, result_json = @ResultJson, error_json = @ErrorJson,
                    last_error_message = @Mensaje, updated_at = @Now
                WHERE transaction_id = @Id;";
            await conn.ExecuteAsync(sql, new { Estado = estado, ResultJson = resultJson, ErrorJson = errorJson, Mensaje = mensaje, Now = Ahora(), Id = transactionId });
        }

        public async Task ProgramarReintentoAsync(string transactionId, int retryCount, DateTime nextAttemptAt, string errorJson, string mensaje)
        {
            using var conn = GetConnection();
            const string sql = @"
                UPDATE bridge_transactions
                SET status = @Pending, retry_count = @RetryCount, next_attempt_at = @Next,
                    error_json = @ErrorJson, last_error_message = @Mensaje, updated_at = @Now
                WHERE transaction_id = @Id;";
            await conn.ExecuteAsync(sql, new
            {
                Pending = Estados.Pending, RetryCount = retryCount, Next = nextAttemptAt.ToString("o"),
                ErrorJson = errorJson, Mensaje = mensaje, Now = Ahora(), Id = transactionId,
            });
        }

        public async Task<bool> ReencolarAsync(string transactionId, string? nuevaCarga)
        {
            using var conn = GetConnection();
            const string sql = @"
                UPDATE bridge_transactions
                SET status = @Pending, retry_count = 0, next_attempt_at = @Now,
                    payload_json = COALESCE(@Carga, payload_json),
                    result_json = NULL, error_json = NULL, last_error_code = NULL, last_error_message = NULL,
                    updated_at = @Now
                WHERE transaction_id = @Id AND status = @DeadLetter;";
            return await conn.ExecuteAsync(sql, new
            {
                Pending = Estados.Pending, DeadLetter = Estados.DeadLetter, Carga = nuevaCarga, Now = Ahora(), Id = transactionId,
            }) > 0;
        }

        public async Task RegistrarEntregaCallbackAsync(string transactionId, string callbackUrl, int? httpStatus, string? respuesta, int intento)
        {
            using var conn = GetConnection();
            const string sql = @"
                INSERT INTO webhook_deliveries (delivery_id, transaction_id, callback_url, http_status, response_body, attempt_count, delivered_at)
                VALUES (@Id, @TransactionId, @Url, @Status, @Respuesta, @Intento, @Now);";
            await conn.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid().ToString(), TransactionId = transactionId, Url = callbackUrl,
                Status = httpStatus, Respuesta = respuesta, Intento = intento, Now = Ahora(),
            });
        }

        public async Task AddLogAsync(TransactionLog log)
        {
            using var conn = GetConnection();
            const string sql = @"
                INSERT INTO transaction_logs (
                    log_id, transaction_id, correlation_id, attempt_number,
                    sdk_function_name, sdk_error_code, error_message, duration_ms, timestamp
                ) VALUES (
                    @LogId, @TransactionId, @CorrelationId, @AttemptNumber,
                    @SdkFunctionName, @SdkErrorCode, @ErrorMessage, @DurationMs, @Timestamp
                );";
            await conn.ExecuteAsync(sql, log);
        }

        public async Task<IEnumerable<BridgeTransaction>> GetDlqTransactionsAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<BridgeTransaction>(
                $"SELECT {SelectFields} FROM bridge_transactions WHERE status = @DeadLetter ORDER BY updated_at DESC;",
                new { DeadLetter = Estados.DeadLetter });
        }

        public async Task<bool> DeleteDlqTransactionAsync(string transactionId)
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(
                "DELETE FROM bridge_transactions WHERE transaction_id = @Id AND status = @DeadLetter;",
                new { Id = transactionId, DeadLetter = Estados.DeadLetter }) > 0;
        }

        public async Task<int> GetQueueDepthAsync()
        {
            using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM bridge_transactions WHERE status IN (@Pending, @Processing);",
                new { Pending = Estados.Pending, Processing = Estados.Processing });
        }

        public async Task<int> DeletePendingTransactionsAsync()
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(
                "DELETE FROM bridge_transactions WHERE status IN (@Pending, @Processing);",
                new { Pending = Estados.Pending, Processing = Estados.Processing });
        }

        public async Task<int> PurgeAllTransactionsAsync()
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync("DELETE FROM bridge_transactions;");
        }

        public async Task<bool> DeleteTransactionAsync(string transactionId)
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync("DELETE FROM bridge_transactions WHERE transaction_id = @Id;", new { Id = transactionId }) > 0;
        }
    }
}

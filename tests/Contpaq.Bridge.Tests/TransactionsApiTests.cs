using System;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Moq;
using Xunit;

namespace Contpaq.Bridge.Tests
{
    public class TransactionsApiTests
    {
        [Fact]
        public void CircuitBreaker_Trips_After_Threshold_Failures()
        {
            var cb = new CircuitBreakerPolicy(threshold: 3, cooldownSeconds: 15);

            Assert.Equal("CLOSED", cb.State);
            Assert.True(cb.AllowExecution());

            cb.RecordFailure();
            cb.RecordFailure();
            Assert.Equal("CLOSED", cb.State);

            cb.RecordFailure(); // 3rd failure
            Assert.Equal("OPEN", cb.State);
            Assert.False(cb.AllowExecution());
        }

        [Fact]
        public async Task OutboxRepository_Add_And_Retrieve_Transaction()
        {
            var dbName = $"Data Source=test_outbox_{Guid.NewGuid():N}.db";
            var initializer = new DbInitializer(dbName);
            initializer.Initialize();

            var repo = new OutboxRepository(dbName);

            var tx = new BridgeTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                ClientAppId = "test-suite",
                CommandType = "DOCUMENT_CREATE",
                PayloadJson = "{\"codigo_concepto\":\"3\"}",
                Status = "PENDING"
            };

            var success = await repo.AddTransactionAsync(tx);
            Assert.True(success);

            var retrieved = await repo.GetByIdAsync(tx.TransactionId);
            Assert.NotNull(retrieved);
            Assert.Equal("test-suite", retrieved!.ClientAppId);
            Assert.Equal("PENDING", retrieved.Status);
        }

        [Fact]
        public async Task OutboxRepository_Rejects_Duplicate_IdempotencyKey()
        {
            var dbName = $"Data Source=test_outbox_{Guid.NewGuid():N}.db";
            new DbInitializer(dbName).Initialize();
            var repo = new OutboxRepository(dbName);

            var tx1 = new BridgeTransaction { IdempotencyKey = "DUP-KEY-100", ClientAppId = "app-1", CommandType = "DOCUMENT_CREATE", PayloadJson = "{}" };
            var tx2 = new BridgeTransaction { IdempotencyKey = "DUP-KEY-100", ClientAppId = "app-2", CommandType = "DOCUMENT_CREATE", PayloadJson = "{}" };

            var res1 = await repo.AddTransactionAsync(tx1);
            var res2 = await repo.AddTransactionAsync(tx2);

            Assert.True(res1);
            Assert.False(res2);
        }
    }
}

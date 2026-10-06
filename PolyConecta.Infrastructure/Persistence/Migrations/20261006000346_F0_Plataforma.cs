using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0_Plataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "plt");

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    LockKeys = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BridgeTransactionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reference_sequence",
                schema: "plt",
                columns: table => new
                {
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Padding = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    ResetRule = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentPeriod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_sequence", x => x.DocumentType);
                });

            migrationBuilder.CreateTable(
                name: "state_transition_log",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_state_transition_log", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "plt",
                table: "reference_sequence",
                columns: new[] { "DocumentType", "CurrentPeriod", "NextNumber", "Padding", "Prefix", "ResetRule" },
                values: new object[] { "ROLLO_EXTRUSION", null, 1L, 6, "EX-{linea}-{yy}", "Anual" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_IdempotencyKey",
                schema: "plt",
                table: "outbox_message",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_Sequence",
                schema: "plt",
                table: "outbox_message",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_Status_Sequence",
                schema: "plt",
                table: "outbox_message",
                columns: new[] { "Status", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_state_transition_log_EntityType_EntityId_OccurredAt",
                schema: "plt",
                table: "state_transition_log",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "reference_sequence",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "state_transition_log",
                schema: "plt");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F1_Sincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_sync_state",
                schema: "plt",
                columns: table => new
                {
                    Catalog = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastResult = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RecordsRead = table.Column<int>(type: "int", nullable: false),
                    RecordsChanged = table.Column<int>(type: "int", nullable: false),
                    RecordsArchived = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_sync_state", x => x.Catalog);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_sync_state",
                schema: "plt");
        }
    }
}

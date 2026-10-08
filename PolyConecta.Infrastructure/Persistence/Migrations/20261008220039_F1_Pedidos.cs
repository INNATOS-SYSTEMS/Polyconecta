using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F1_Pedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sales_order",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerPo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    AgentId = table.Column<long>(type: "bigint", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PromiseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryAddressId = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryAddressText = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    sync_status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    erp_folio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    erp_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    erp_documents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sync_last_error_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    sync_last_error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sync_last_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_order_customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ven",
                        principalTable: "customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_customer_address_DeliveryAddressId",
                        column: x => x.DeliveryAddressId,
                        principalSchema: "ven",
                        principalTable: "customer_address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_erp_agent_AgentId",
                        column: x => x.AgentId,
                        principalSchema: "ven",
                        principalTable: "erp_agent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authorization_signature",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GroupExercised = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsSubstitute = table.Column<bool>(type: "bit", nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_signature", x => x.Id);
                    table.ForeignKey(
                        name: "FK_authorization_signature_sales_order_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "ven",
                        principalTable: "sales_order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_authorization_signature_user_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_line",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RequestedPackagingUnitId = table.Column<long>(type: "bigint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TargetProductionKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TolerancePercentageOverride = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ForcedRouteId = table.Column<long>(type: "bigint", nullable: true),
                    ErpDocumentLineId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_line", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_order_line_packaging_unit_RequestedPackagingUnitId",
                        column: x => x.RequestedPackagingUnitId,
                        principalSchema: "inv",
                        principalTable: "packaging_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_line_product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_line_sales_order_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "ven",
                        principalTable: "sales_order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "plt",
                table: "reference_sequence",
                columns: new[] { "DocumentType", "CurrentPeriod", "NextNumber", "Padding", "Prefix", "ResetRule" },
                values: new object[] { "PEDIDO_VENTA", null, 1L, 4, "PV-{yyyy}-", "Anual" });

            migrationBuilder.CreateIndex(
                name: "IX_authorization_signature_SalesOrderId_Role",
                schema: "ven",
                table: "authorization_signature",
                columns: new[] { "SalesOrderId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_authorization_signature_SalesOrderId_UserId",
                schema: "ven",
                table: "authorization_signature",
                columns: new[] { "SalesOrderId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_authorization_signature_UserId",
                schema: "ven",
                table: "authorization_signature",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_AgentId",
                schema: "ven",
                table: "sales_order",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_CustomerId",
                schema: "ven",
                table: "sales_order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_DeliveryAddressId",
                schema: "ven",
                table: "sales_order",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_Name",
                schema: "ven",
                table: "sales_order",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_State",
                schema: "ven",
                table: "sales_order",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_ProductId",
                schema: "ven",
                table: "sales_order_line",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_RequestedPackagingUnitId",
                schema: "ven",
                table: "sales_order_line",
                column: "RequestedPackagingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_SalesOrderId",
                schema: "ven",
                table: "sales_order_line",
                column: "SalesOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_signature",
                schema: "ven");

            migrationBuilder.DropTable(
                name: "sales_order_line",
                schema: "ven");

            migrationBuilder.DropTable(
                name: "sales_order",
                schema: "ven");

            migrationBuilder.DeleteData(
                schema: "plt",
                table: "reference_sequence",
                keyColumn: "DocumentType",
                keyValue: "PEDIDO_VENTA");
        }
    }
}

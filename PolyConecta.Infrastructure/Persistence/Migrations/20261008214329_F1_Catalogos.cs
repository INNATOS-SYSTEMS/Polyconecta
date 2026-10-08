using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F1_Catalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ven");

            migrationBuilder.CreateTable(
                name: "customer",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ErpCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "erp_agent",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpAgentId = table.Column<long>(type: "bigint", nullable: false),
                    ErpCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_erp_agent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "erp_warehouse",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpWarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ErpCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_erp_warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_classification",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ErpValue = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_classification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_address",
                schema: "ven",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ErpAddressId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExteriorNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InteriorNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Branch = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_address_customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ven",
                        principalTable: "customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpProductId = table.Column<long>(type: "bigint", nullable: false),
                    ErpCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ErpUom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TracksLots = table.Column<bool>(type: "bit", nullable: false),
                    ClassificationId = table.Column<long>(type: "bigint", nullable: true),
                    ErpSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_product_classification_ClassificationId",
                        column: x => x.ClassificationId,
                        principalSchema: "inv",
                        principalTable: "product_classification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "packaging_unit",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsErpBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packaging_unit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_packaging_unit_product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roll_specification",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    MaterialType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RollTypeSize = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GaugeMicrons = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    KgPerRoll = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    TreatmentDynes = table.Column<int>(type: "int", nullable: true),
                    Pigment = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Additive = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Perforation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PreliminaryPrint = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roll_specification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roll_specification_product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pt_specification",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    RelatedRollSpecificationId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerPartNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FinalSize = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Inks = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Pantones = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DieCut = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Packaging = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SealType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    KgPerThousand = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pt_specification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pt_specification_product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pt_specification_roll_specification_RelatedRollSpecificationId",
                        column: x => x.RelatedRollSpecificationId,
                        principalSchema: "inv",
                        principalTable: "roll_specification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_ErpAgentId",
                schema: "plt",
                table: "user",
                column: "ErpAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ErpCode",
                schema: "ven",
                table: "customer",
                column: "ErpCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_ErpCustomerId",
                schema: "ven",
                table: "customer",
                column: "ErpCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_address_CustomerId",
                schema: "ven",
                table: "customer_address",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_address_ErpAddressId",
                schema: "ven",
                table: "customer_address",
                column: "ErpAddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_erp_agent_ErpAgentId",
                schema: "ven",
                table: "erp_agent",
                column: "ErpAgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_erp_warehouse_ErpWarehouseId",
                schema: "inv",
                table: "erp_warehouse",
                column: "ErpWarehouseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_packaging_unit_ProductId_Code",
                schema: "inv",
                table: "packaging_unit",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ClassificationId",
                schema: "inv",
                table: "product",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_product_ErpCode",
                schema: "inv",
                table: "product",
                column: "ErpCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ErpProductId",
                schema: "inv",
                table: "product",
                column: "ErpProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_classification_Code",
                schema: "inv",
                table: "product_classification",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pt_specification_ProductId",
                schema: "inv",
                table: "pt_specification",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pt_specification_RelatedRollSpecificationId",
                schema: "inv",
                table: "pt_specification",
                column: "RelatedRollSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_roll_specification_ProductId",
                schema: "inv",
                table: "roll_specification",
                column: "ProductId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_erp_agent_ErpAgentId",
                schema: "plt",
                table: "user",
                column: "ErpAgentId",
                principalSchema: "ven",
                principalTable: "erp_agent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_erp_agent_ErpAgentId",
                schema: "plt",
                table: "user");

            migrationBuilder.DropTable(
                name: "customer_address",
                schema: "ven");

            migrationBuilder.DropTable(
                name: "erp_agent",
                schema: "ven");

            migrationBuilder.DropTable(
                name: "erp_warehouse",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "packaging_unit",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "pt_specification",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "customer",
                schema: "ven");

            migrationBuilder.DropTable(
                name: "roll_specification",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "product",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "product_classification",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_user_ErpAgentId",
                schema: "plt",
                table: "user");
        }
    }
}

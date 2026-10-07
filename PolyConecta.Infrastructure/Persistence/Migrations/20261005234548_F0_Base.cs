using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0_Base : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "prd");

            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.EnsureSchema(
                name: "cal");

            migrationBuilder.CreateTable(
                name: "poly_locations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CidAlmacenContpaq = table.Column<int>(type: "int", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlantCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    WarehouseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poly_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poly_lot_genealogy",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentLotNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChildLotNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceRollId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRollId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityConsumedKg = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poly_lot_genealogy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poly_mass_balance_audits",
                schema: "prd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalMpInputKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    TotalRollOutputKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    TotalScrapOutputKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    VariancePercentage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: false),
                    AuditPassed = table.Column<bool>(type: "bit", nullable: false),
                    FlagReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poly_mass_balance_audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poly_raw_material_catalogs",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MfiMeltFlowIndex = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DensityGcm3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetHopper = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CidProductoContpaq = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poly_raw_material_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContpaqProductId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLocations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContpaqWarehouseId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poly_supplier_product_mappings",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawMaterialCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MappedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poly_supplier_product_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poly_supplier_product_mappings_poly_raw_material_catalogs_RawMaterialCatalogId",
                        column: x => x.RawMaterialCatalogId,
                        principalSchema: "inv",
                        principalTable: "poly_raw_material_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Boms",
                schema: "prd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LayerAPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LayerBPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LayerCPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boms_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManufacturingOrders",
                schema: "prd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContpaqDocumentId = table.Column<int>(type: "int", nullable: true),
                    CustomerCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductQtyTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductQtyProduced = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalesApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreditApproved = table.Column<bool>(type: "bit", nullable: false),
                    WorkCenterId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManufacturingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManufacturingOrders_ManufacturingOrders_ParentOrderId",
                        column: x => x.ParentOrderId,
                        principalSchema: "prd",
                        principalTable: "ManufacturingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManufacturingOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockPickings",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationDestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPickings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPickings_StockLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalSchema: "inv",
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPickings_StockLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "inv",
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomLines",
                schema: "prd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Layer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComponentPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomLines_Boms_BomId",
                        column: x => x.BomId,
                        principalSchema: "prd",
                        principalTable: "Boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockLots",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductSku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContpaqLotNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrossWeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TareWeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LengthMeters = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GaugeMicron = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WidthMm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DynesCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MachineId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLocationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProducedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLots_ManufacturingOrders_ManufacturingOrderId",
                        column: x => x.ManufacturingOrderId,
                        principalSchema: "prd",
                        principalTable: "ManufacturingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockScraps",
                schema: "prd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScrapReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockScraps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockScraps_ManufacturingOrders_ManufacturingOrderId",
                        column: x => x.ManufacturingOrderId,
                        principalSchema: "prd",
                        principalTable: "ManufacturingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockScraps_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityChecks",
                schema: "cal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManufacturingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GaugeMeasured = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DynesMeasured = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VisualInspectionPassed = table.Column<bool>(type: "bit", nullable: false),
                    QualityState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InspectorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityChecks_ManufacturingOrders_ManufacturingOrderId",
                        column: x => x.ManufacturingOrderId,
                        principalSchema: "prd",
                        principalTable: "ManufacturingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityChecks_StockLots_StockLotId",
                        column: x => x.StockLotId,
                        principalSchema: "inv",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMoves",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockPickingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManufacturingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductUomQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationDestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMoves_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inv",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMoves_StockLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalSchema: "inv",
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMoves_StockLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "inv",
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMoves_StockLots_StockLotId",
                        column: x => x.StockLotId,
                        principalSchema: "inv",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMoves_StockPickings_StockPickingId",
                        column: x => x.StockPickingId,
                        principalSchema: "inv",
                        principalTable: "StockPickings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "inv",
                table: "poly_locations",
                columns: new[] { "Id", "CidAlmacenContpaq", "IsActive", "LocationCode", "LocationName", "PlantCode", "WarehouseType" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 1, true, "PIM/Stock/MP", "Apodaca MP Stock", "PIM", "RawMaterial" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 2, true, "PIM/Produccion", "Apodaca WIP Production", "PIM", "Production" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 3, true, "PIM/Stock/PT", "Apodaca PT Stock", "PIM", "FinishedGoods" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 4, true, "PIM/Cuarentena", "Apodaca Quality Quarantine", "PIM", "Quarantine" }
                });

            migrationBuilder.InsertData(
                schema: "inv",
                table: "poly_raw_material_catalogs",
                columns: new[] { "Id", "Category", "CidProductoContpaq", "CreatedAt", "DensityGcm3", "InternalSku", "IsActive", "MfiMeltFlowIndex", "Name", "TargetHopper" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), "VirginResin", 101, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), 0.958m, "MP-RES-HD-001", true, 0.95m, "Resina PE Alta Densidad Alathon M6210", "Tolva A" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "VirginResin", 102, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), 0.922m, "MP-RES-LD-002", true, 2.00m, "Resina PE Baja Densidad Braskem BC818", "Tolva B" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "Additive", 103, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), 0.940m, "MP-ADI-UV-001", true, 1.10m, "Masterbatch Aditivo Anti-UV Clariant", "Tolva C" }
                });

            migrationBuilder.InsertData(
                schema: "inv",
                table: "poly_supplier_product_mappings",
                columns: new[] { "Id", "MappedAt", "RawMaterialCatalogId", "SupplierCode", "SupplierProductName", "SupplierSku" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), "LYONDELL", "Alathon M6210 High Density Polyethylene", "LYO-M6210-HD" },
                    { new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), "BRASKEM", "Braskem BC818 Low Density Polyethylene", "BRASK-BC818" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomLines_BomId",
                schema: "prd",
                table: "BomLines",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_BomLines_ProductId",
                schema: "prd",
                table: "BomLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Boms_ProductId",
                schema: "prd",
                table: "Boms",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturingOrders_ParentOrderId",
                schema: "prd",
                table: "ManufacturingOrders",
                column: "ParentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturingOrders_ProductId",
                schema: "prd",
                table: "ManufacturingOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_poly_locations_CidAlmacenContpaq",
                schema: "inv",
                table: "poly_locations",
                column: "CidAlmacenContpaq",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poly_locations_LocationCode",
                schema: "inv",
                table: "poly_locations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poly_raw_material_catalogs_InternalSku",
                schema: "inv",
                table: "poly_raw_material_catalogs",
                column: "InternalSku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poly_supplier_product_mappings_RawMaterialCatalogId",
                schema: "inv",
                table: "poly_supplier_product_mappings",
                column: "RawMaterialCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_ManufacturingOrderId",
                schema: "cal",
                table: "QualityChecks",
                column: "ManufacturingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_StockLotId",
                schema: "cal",
                table: "QualityChecks",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ManufacturingOrderId",
                schema: "inv",
                table: "StockLots",
                column: "ManufacturingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ProductId",
                schema: "inv",
                table: "StockLots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMoves_DestinationLocationId",
                schema: "inv",
                table: "StockMoves",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMoves_ProductId",
                schema: "inv",
                table: "StockMoves",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMoves_SourceLocationId",
                schema: "inv",
                table: "StockMoves",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMoves_StockLotId",
                schema: "inv",
                table: "StockMoves",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMoves_StockPickingId",
                schema: "inv",
                table: "StockMoves",
                column: "StockPickingId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPickings_DestinationLocationId",
                schema: "inv",
                table: "StockPickings",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPickings_SourceLocationId",
                schema: "inv",
                table: "StockPickings",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockScraps_ManufacturingOrderId",
                schema: "prd",
                table: "StockScraps",
                column: "ManufacturingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockScraps_ProductId",
                schema: "prd",
                table: "StockScraps",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomLines",
                schema: "prd");

            migrationBuilder.DropTable(
                name: "poly_locations",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "poly_lot_genealogy",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "poly_mass_balance_audits",
                schema: "prd");

            migrationBuilder.DropTable(
                name: "poly_supplier_product_mappings",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "QualityChecks",
                schema: "cal");

            migrationBuilder.DropTable(
                name: "StockMoves",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockScraps",
                schema: "prd");

            migrationBuilder.DropTable(
                name: "Boms",
                schema: "prd");

            migrationBuilder.DropTable(
                name: "poly_raw_material_catalogs",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockLots",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockPickings",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ManufacturingOrders",
                schema: "prd");

            migrationBuilder.DropTable(
                name: "StockLocations",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "inv");
        }
    }
}

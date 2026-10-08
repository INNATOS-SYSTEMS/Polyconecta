using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyConecta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F1_Seguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "group",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Object = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObjectKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModuleLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plant",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ErpAgentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_permission",
                schema: "plt",
                columns: table => new
                {
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_permission", x => new { x.GroupId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_group_permission_group_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plt",
                        principalTable: "group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_permission_permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "plt",
                        principalTable: "permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_assignment",
                schema: "plt",
                columns: table => new
                {
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    PlantId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsSubstitute = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_assignment", x => new { x.UserId, x.GroupId, x.PlantId });
                    table.ForeignKey(
                        name: "FK_group_assignment_group_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plt",
                        principalTable: "group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_assignment_plant_PlantId",
                        column: x => x.PlantId,
                        principalSchema: "plt",
                        principalTable: "plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_assignment_user_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_credential",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credential", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_credential_user_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_credential_claim",
                schema: "plt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credential_claim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_credential_claim_user_credential_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user_credential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_credential_login",
                schema: "plt",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credential_login", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_credential_login_user_credential_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user_credential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_credential_token",
                schema: "plt",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credential_token", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_credential_token_user_credential_UserId",
                        column: x => x.UserId,
                        principalSchema: "plt",
                        principalTable: "user_credential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_group_Code",
                schema: "plt",
                table: "group",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_Name",
                schema: "plt",
                table: "group",
                column: "Name",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_group_assignment_GroupId",
                schema: "plt",
                table: "group_assignment",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_assignment_PlantId",
                schema: "plt",
                table: "group_assignment",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_group_permission_PermissionId",
                schema: "plt",
                table: "group_permission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_permission_Key",
                schema: "plt",
                table: "permission",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plant_Code",
                schema: "plt",
                table: "plant",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_UserName",
                schema: "plt",
                table: "user",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "plt",
                table: "user_credential",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_user_credential_UserId",
                schema: "plt",
                table: "user_credential",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "plt",
                table: "user_credential",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_credential_claim_UserId",
                schema: "plt",
                table: "user_credential_claim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_credential_login_UserId",
                schema: "plt",
                table: "user_credential_login",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_assignment",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "group_permission",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "user_credential_claim",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "user_credential_login",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "user_credential_token",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "plant",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "group",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "permission",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "user_credential",
                schema: "plt");

            migrationBuilder.DropTable(
                name: "user",
                schema: "plt");
        }
    }
}

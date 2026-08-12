using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class KeyAccessCopy1 : Migration
    {
        private static readonly string[] KeyNumberMedecoColumns = ["KeyNumber", "MedecoKeyCode"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM KeyAssets)
                   OR EXISTS (SELECT 1 FROM KeyRoomAssignments)
                   OR EXISTS (SELECT 1 FROM Loans)
                BEGIN
                    THROW 50001, 'Migration stopped: KeyAssets, KeyRoomAssignments, or Loans contain rows. CatalogKeyCode cannot be mapped to KEY # / MEDECO without non-deterministic parsing. Clear those tables under an authorized demo/test reset, or provide an explicit mapping procedure, before retrying KEY-ACCESS-COPY-1.', 1;
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_KeyAssets_CatalogKeyCode",
                table: "Loans");

            migrationBuilder.DropTable(
                name: "KeyRoomAssignments");

            migrationBuilder.DropTable(
                name: "KeyAssets");

            migrationBuilder.DropIndex(
                name: "IX_Loans_CatalogKeyCode",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "CatalogKeyCode",
                table: "Loans");

            migrationBuilder.AddColumn<Guid>(
                name: "KeyAssetId",
                table: "Loans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "KeyAccessPatterns",
                columns: table => new
                {
                    KeyNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    KeyTypeCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyAccessPatterns", x => x.KeyNumber);
                    table.ForeignKey(
                        name: "FK_KeyAccessPatterns_KeyTypes_KeyTypeCode",
                        column: x => x.KeyTypeCode,
                        principalTable: "KeyTypes",
                        principalColumn: "TypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyAssets",
                columns: table => new
                {
                    KeyAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MedecoKeyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyAssets", x => x.KeyAssetId);
                    table.ForeignKey(
                        name: "FK_KeyAssets_KeyAccessPatterns_KeyNumber",
                        column: x => x.KeyNumber,
                        principalTable: "KeyAccessPatterns",
                        principalColumn: "KeyNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyAccessPatternRoomAssignments",
                columns: table => new
                {
                    KeyNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyAccessPatternRoomAssignments", x => new { x.KeyNumber, x.RoomCode });
                    table.ForeignKey(
                        name: "FK_KeyAccessPatternRoomAssignments_KeyAccessPatterns_KeyNumber",
                        column: x => x.KeyNumber,
                        principalTable: "KeyAccessPatterns",
                        principalColumn: "KeyNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyAccessPatternRoomAssignments_Rooms_RoomCode",
                        column: x => x.RoomCode,
                        principalTable: "Rooms",
                        principalColumn: "RoomCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_KeyAssetId",
                table: "Loans",
                column: "KeyAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyAssets_KeyNumber_MedecoKeyCode",
                table: "KeyAssets",
                columns: KeyNumberMedecoColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyAccessPatternRoomAssignments_RoomCode",
                table: "KeyAccessPatternRoomAssignments",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_KeyAccessPatterns_KeyTypeCode",
                table: "KeyAccessPatterns",
                column: "KeyTypeCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_KeyAssets_KeyAssetId",
                table: "Loans",
                column: "KeyAssetId",
                principalTable: "KeyAssets",
                principalColumn: "KeyAssetId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM KeyAssets)
                   OR EXISTS (SELECT 1 FROM KeyAccessPatternRoomAssignments)
                   OR EXISTS (SELECT 1 FROM Loans)
                BEGIN
                    THROW 50002, 'Down migration stopped: KeyAssets, KeyAccessPatternRoomAssignments, or Loans contain rows. Reverting KEY-ACCESS-COPY-1 would require non-deterministic CatalogKeyCode reconstruction.', 1;
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_KeyAssets_KeyAssetId",
                table: "Loans");

            migrationBuilder.DropTable(
                name: "KeyAccessPatternRoomAssignments");

            migrationBuilder.DropTable(
                name: "KeyAssets");

            migrationBuilder.DropTable(
                name: "KeyAccessPatterns");

            migrationBuilder.DropIndex(
                name: "IX_Loans_KeyAssetId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "KeyAssetId",
                table: "Loans");

            migrationBuilder.AddColumn<string>(
                name: "CatalogKeyCode",
                table: "Loans",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "KeyAssets",
                columns: table => new
                {
                    CatalogKeyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    KeyTypeCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyAssets", x => x.CatalogKeyCode);
                    table.ForeignKey(
                        name: "FK_KeyAssets_KeyTypes_KeyTypeCode",
                        column: x => x.KeyTypeCode,
                        principalTable: "KeyTypes",
                        principalColumn: "TypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyRoomAssignments",
                columns: table => new
                {
                    CatalogKeyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyRoomAssignments", x => new { x.CatalogKeyCode, x.RoomCode });
                    table.ForeignKey(
                        name: "FK_KeyRoomAssignments_KeyAssets_CatalogKeyCode",
                        column: x => x.CatalogKeyCode,
                        principalTable: "KeyAssets",
                        principalColumn: "CatalogKeyCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyRoomAssignments_Rooms_RoomCode",
                        column: x => x.RoomCode,
                        principalTable: "Rooms",
                        principalColumn: "RoomCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CatalogKeyCode",
                table: "Loans",
                column: "CatalogKeyCode");

            migrationBuilder.CreateIndex(
                name: "IX_KeyAssets_KeyTypeCode",
                table: "KeyAssets",
                column: "KeyTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_KeyRoomAssignments_RoomCode",
                table: "KeyRoomAssignments",
                column: "RoomCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_KeyAssets_CatalogKeyCode",
                table: "Loans",
                column: "CatalogKeyCode",
                principalTable: "KeyAssets",
                principalColumn: "CatalogKeyCode",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

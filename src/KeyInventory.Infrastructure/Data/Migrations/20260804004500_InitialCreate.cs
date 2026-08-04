using Microsoft.EntityFrameworkCore.Migrations;

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
            name: "KeyTypes",
            columns: table => new
            {
                TypeCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KeyTypes", x => x.TypeCode);
            });

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
            name: "Loans",
            columns: table => new
            {
                LoanCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CatalogKeyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                BorrowerPartyReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Loans", x => x.LoanCode);
                table.ForeignKey(
                    name: "FK_Loans_KeyAssets_CatalogKeyCode",
                    column: x => x.CatalogKeyCode,
                    principalTable: "KeyAssets",
                    principalColumn: "CatalogKeyCode",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Returns",
            columns: table => new
            {
                ReturnCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                LoanCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Returns", x => x.ReturnCode);
                table.ForeignKey(
                    name: "FK_Returns_Loans_LoanCode",
                    column: x => x.LoanCode,
                    principalTable: "Loans",
                    principalColumn: "LoanCode",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_KeyAssets_KeyTypeCode",
            table: "KeyAssets",
            column: "KeyTypeCode");

        migrationBuilder.CreateIndex(
            name: "IX_Loans_CatalogKeyCode",
            table: "Loans",
            column: "CatalogKeyCode");

        migrationBuilder.CreateIndex(
            name: "IX_Returns_LoanCode",
            table: "Returns",
            column: "LoanCode",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(
            name: "Returns");

        migrationBuilder.DropTable(
            name: "Loans");

        migrationBuilder.DropTable(
            name: "KeyAssets");

        migrationBuilder.DropTable(
            name: "KeyTypes");
    }
}

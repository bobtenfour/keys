using System;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations;

[DbContext(typeof(KeyInventoryDbContext))]
[Migration("20260814210000_KeyAssetPhysicalConditionAndReplacement")]
public class KeyAssetPhysicalConditionAndReplacement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // Development data disposable: clear custody/catalog rows that depended on IsActive.
        migrationBuilder.Sql("DELETE FROM [Returns];");
        migrationBuilder.Sql("DELETE FROM [Loans];");
        migrationBuilder.Sql("DELETE FROM [KeyAssets];");

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "KeyAssets");

        migrationBuilder.AddColumn<string>(
            name: "Condition",
            table: "KeyAssets",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Active");

        migrationBuilder.AddColumn<Guid>(
            name: "ReplacesKeyAssetId",
            table: "KeyAssets",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_KeyAssets_Condition",
            table: "KeyAssets",
            column: "Condition");

        migrationBuilder.CreateIndex(
            name: "IX_KeyAssets_ReplacesKeyAssetId",
            table: "KeyAssets",
            column: "ReplacesKeyAssetId");

        migrationBuilder.AddForeignKey(
            name: "FK_KeyAssets_KeyAssets_ReplacesKeyAssetId",
            table: "KeyAssets",
            column: "ReplacesKeyAssetId",
            principalTable: "KeyAssets",
            principalColumn: "KeyAssetId",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "KeyAssetPhysicalConditionAndReplacement is forward-only; Down is not supported.");
    }
}

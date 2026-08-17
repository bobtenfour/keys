using System;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations;

[DbContext(typeof(KeyInventoryDbContext))]
[Migration("20260816180000_ClassificationDefinesKeyAccess")]
public class ClassificationDefinesKeyAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // Prefer: for Regular KEY #s, take first RoomCode from join before drop; Master stays null.
        migrationBuilder.AddColumn<string>(
            name: "RoomCode",
            table: "KeyAccessPatterns",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE kap
            SET kap.RoomCode = src.RoomCode
            FROM [KeyAccessPatterns] kap
            INNER JOIN (
                SELECT assignment.KeyNumber, MIN(assignment.RoomCode) AS RoomCode
                FROM [KeyAccessPatternRoomAssignments] assignment
                GROUP BY assignment.KeyNumber
            ) src ON src.KeyNumber = kap.KeyNumber
            WHERE kap.Classification = N'Regular';
            """);

        migrationBuilder.Sql("""
            UPDATE [KeyAccessPatterns]
            SET [RoomCode] = NULL
            WHERE [Classification] = N'Master';
            """);

        migrationBuilder.DropTable(
            name: "KeyAccessPatternRoomAssignments");

        migrationBuilder.CreateIndex(
            name: "IX_KeyAccessPatterns_RoomCode",
            table: "KeyAccessPatterns",
            column: "RoomCode");

        migrationBuilder.AddForeignKey(
            name: "FK_KeyAccessPatterns_Rooms_RoomCode",
            table: "KeyAccessPatterns",
            column: "RoomCode",
            principalTable: "Rooms",
            principalColumn: "RoomCode",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "ClassificationDefinesKeyAccess is forward-only; Down is not supported.");
    }
}

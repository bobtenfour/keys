using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class NormalizedRoomDepartmentKeyClassification : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // 0) Development data is disposable per human rule: any row that cannot be mapped to the
        //    final normalized model (Rooms require DepartmentId, KEY # requires Regular/Master
        //    classification) is deleted rather than guessed. Order respects FKs; Departments,
        //    Parties, WorkforceMembers, and Identity tables are preserved.
        migrationBuilder.Sql("DELETE FROM [Returns];");
        migrationBuilder.Sql("DELETE FROM [Loans];");
        migrationBuilder.Sql("DELETE FROM [KeyAccessPatternRoomAssignments];");
        migrationBuilder.Sql("DELETE FROM [KeyAssets];");
        migrationBuilder.Sql("DELETE FROM [KeyAccessPatterns];");
        migrationBuilder.Sql("DELETE FROM [WorkAssignments];");
        migrationBuilder.Sql("DELETE FROM [Rooms];");

        // 1) Sever KeyAccessPatterns → KeyTypes and drop the KeyTypes authority entirely.
        migrationBuilder.DropForeignKey(
            name: "FK_KeyAccessPatterns_KeyTypes_KeyTypeCode",
            table: "KeyAccessPatterns");

        migrationBuilder.DropIndex(
            name: "IX_KeyAccessPatterns_KeyTypeCode",
            table: "KeyAccessPatterns");

        migrationBuilder.DropColumn(
            name: "KeyTypeCode",
            table: "KeyAccessPatterns");

        migrationBuilder.DropTable(
            name: "KeyTypes");

        // 2) Introduce Classification as the sole KEY # access classification authority.
        //    Persisted as the enum string ("Regular" | "Master") for schema clarity. The
        //    KeyAccessPatterns table is empty at this point, so NOT NULL is added directly.
        migrationBuilder.AddColumn<string>(
            name: "Classification",
            table: "KeyAccessPatterns",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: string.Empty);

        // 3) Rooms.DepartmentId (required FK to Departments, Restrict). Table emptied above so
        //    NOT NULL is added directly with an empty-guid literal that no row references.
        migrationBuilder.AddColumn<Guid>(
            name: "DepartmentId",
            table: "Rooms",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_Rooms_DepartmentId",
            table: "Rooms",
            column: "DepartmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_Rooms_Departments_DepartmentId",
            table: "Rooms",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "DepartmentId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "NormalizedRoomDepartmentKeyClassification cannot be reversed. KeyType authority and "
            + "Rooms-without-Department are not recoverable from the normalized schema. Restore from "
            + "backup if rollback is required.");
    }
}

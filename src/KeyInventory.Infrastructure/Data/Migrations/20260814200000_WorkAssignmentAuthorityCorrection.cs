using System;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(KeyInventoryDbContext))]
[Migration("20260814200000_WorkAssignmentAuthorityCorrection")]
public class WorkAssignmentAuthorityCorrection : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // Development data is disposable: rebuild WorkAssignments without WorkAssignmentCode/IsPrimary.
        migrationBuilder.Sql("DELETE FROM [WorkAssignments];");

        migrationBuilder.DropTable(name: "WorkAssignments");

        migrationBuilder.CreateTable(
            name: "WorkAssignments",
            columns: table => new
            {
                WorkAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkforceMemberCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                RoomCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkAssignments", x => x.WorkAssignmentId);
                table.ForeignKey(
                    name: "FK_WorkAssignments_Rooms_RoomCode",
                    column: x => x.RoomCode,
                    principalTable: "Rooms",
                    principalColumn: "RoomCode",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_WorkAssignments_WorkforceMembers_WorkforceMemberCode",
                    column: x => x.WorkforceMemberCode,
                    principalTable: "WorkforceMembers",
                    principalColumn: "WorkforceMemberCode",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WorkAssignments_RoomCode",
            table: "WorkAssignments",
            column: "RoomCode");

        migrationBuilder.CreateIndex(
            name: "IX_WorkAssignments_WorkforceMemberCode",
            table: "WorkAssignments",
            column: "WorkforceMemberCode");

        migrationBuilder.CreateIndex(
            name: "IX_WorkAssignments_WorkforceMemberCode_RoomCode",
            table: "WorkAssignments",
            columns: WorkAssignmentMemberRoomColumns,
            unique: true,
            filter: "[IsActive] = 1");
    }

    private static readonly string[] WorkAssignmentMemberRoomColumns = ["WorkforceMemberCode", "RoomCode"];

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "WorkAssignmentAuthorityCorrection is a forward-only schema correction; Down is not supported.");
    }
}

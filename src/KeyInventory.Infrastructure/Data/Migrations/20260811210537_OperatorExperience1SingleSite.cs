using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperatorExperience1SingleSite : Migration
    {
        private static readonly string[] DepartmentsOrganizationDepartmentColumns = ["OrganizationCode", "DepartmentCode"];
        private static readonly string[] RoomsBuildingRoomNumberColumns = ["BuildingCode", "RoomNumber"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM Rooms
                    GROUP BY RoomNumber
                    HAVING COUNT(*) > 1)
                BEGIN
                    DECLARE @roomConflicts NVARCHAR(4000);
                    SELECT @roomConflicts = STRING_AGG(RoomNumber + ' (' + CAST(RoomCount AS NVARCHAR(10)) + ' rooms)', '; ')
                    FROM (
                        SELECT RoomNumber, COUNT(*) AS RoomCount
                        FROM Rooms
                        GROUP BY RoomNumber
                        HAVING COUNT(*) > 1) AS Conflicts;
                    RAISERROR(
                        'Migration stopped: duplicate RoomNumber values across buildings prevent global uniqueness. Resolve conflicts before retrying: %s',
                        16,
                        1,
                        @roomConflicts);
                END
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM Departments
                    GROUP BY DepartmentCode
                    HAVING COUNT(*) > 1)
                BEGIN
                    DECLARE @departmentConflicts NVARCHAR(4000);
                    SELECT @departmentConflicts = STRING_AGG(DepartmentCode + ' (' + CAST(DepartmentCount AS NVARCHAR(10)) + ' rows)', '; ')
                    FROM (
                        SELECT DepartmentCode, COUNT(*) AS DepartmentCount
                        FROM Departments
                        GROUP BY DepartmentCode
                        HAVING COUNT(*) > 1) AS Conflicts;
                    RAISERROR(
                        'Migration stopped: duplicate DepartmentCode values across organizations prevent single-site department identity. Resolve conflicts before retrying: %s',
                        16,
                        1,
                        @departmentConflicts);
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Organizations_OrganizationCode",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Buildings_BuildingCode",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_BuildingCode_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Departments",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "WorkforceMembers");

            migrationBuilder.DropColumn(
                name: "ResponsibleManagerWorkforceMemberCode",
                table: "WorkforceMembers");

            migrationBuilder.DropColumn(
                name: "BuildingCode",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Departments",
                table: "Departments",
                column: "DepartmentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms",
                column: "RoomNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Departments",
                table: "Departments");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "WorkforceMembers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleManagerWorkforceMemberCode",
                table: "WorkforceMembers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingCode",
                table: "Rooms",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Departments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Departments",
                table: "Departments",
                columns: DepartmentsOrganizationDepartmentColumns);

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    BuildingCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.BuildingCode);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    OrganizationCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrganizationCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingCode_RoomNumber",
                table: "Rooms",
                columns: RoomsBuildingRoomNumberColumns,
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Organizations_OrganizationCode",
                table: "Departments",
                column: "OrganizationCode",
                principalTable: "Organizations",
                principalColumn: "OrganizationCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Buildings_BuildingCode",
                table: "Rooms",
                column: "BuildingCode",
                principalTable: "Buildings",
                principalColumn: "BuildingCode",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

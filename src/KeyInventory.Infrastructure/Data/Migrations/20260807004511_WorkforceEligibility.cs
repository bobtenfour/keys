using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkforceEligibility : Migration
    {
        private static readonly string[] RoomsBuildingCodeRoomNumberColumns = ["BuildingCode", "RoomNumber"];
        private static readonly string[] WorkAssignmentsMemberPrimaryColumns = ["WorkforceMemberCode", "IsPrimary"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

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

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    PartyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Uin = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.PartyCode);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignments",
                columns: table => new
                {
                    WorkAssignmentCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkforceMemberCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignments", x => x.WorkAssignmentCode);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BuildingCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomCode);
                    table.ForeignKey(
                        name: "FK_Rooms_Buildings_BuildingCode",
                        column: x => x.BuildingCode,
                        principalTable: "Buildings",
                        principalColumn: "BuildingCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    OrganizationCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => new { x.OrganizationCode, x.DepartmentCode });
                    table.ForeignKey(
                        name: "FK_Departments_Organizations_OrganizationCode",
                        column: x => x.OrganizationCode,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceMembers",
                columns: table => new
                {
                    WorkforceMemberCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PartyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkforceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResponsibleManagerWorkforceMemberCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceMembers", x => x.WorkforceMemberCode);
                    table.ForeignKey(
                        name: "FK_WorkforceMembers_Parties_PartyCode",
                        column: x => x.PartyCode,
                        principalTable: "Parties",
                        principalColumn: "PartyCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Uin",
                table: "Parties",
                column: "Uin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingCode_RoomNumber",
                table: "Rooms",
                columns: RoomsBuildingCodeRoomNumberColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_WorkforceMemberCode_IsPrimary",
                table: "WorkAssignments",
                columns: WorkAssignmentsMemberPrimaryColumns,
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceMembers_PartyCode",
                table: "WorkforceMembers",
                column: "PartyCode",
                unique: true,
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "WorkAssignments");

            migrationBuilder.DropTable(
                name: "WorkforceMembers");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Parties");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class KeyRoomAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
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
                name: "IX_KeyRoomAssignments_RoomCode",
                table: "KeyRoomAssignments",
                column: "RoomCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.DropTable(
                name: "KeyRoomAssignments");
        }
    }
}

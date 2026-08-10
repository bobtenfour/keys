using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperatorAuditRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.CreateTable(
                name: "OperatorAuditRecords",
                columns: table => new
                {
                    AuditRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperatorReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SubjectType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubjectReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorAuditRecords", x => x.AuditRecordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAuditRecords_ActionType",
                table: "OperatorAuditRecords",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAuditRecords_OccurredAtUtc",
                table: "OperatorAuditRecords",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAuditRecords_OperatorReference",
                table: "OperatorAuditRecords",
                column: "OperatorReference");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAuditRecords_SubjectReference",
                table: "OperatorAuditRecords",
                column: "SubjectReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.DropTable(
                name: "OperatorAuditRecords");
        }
    }
}

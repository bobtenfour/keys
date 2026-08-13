using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class DepartmentIdentityNormalization : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // 1) Departments.DepartmentId
        // Separate batches: SQL Server cannot compile ADD + UPDATE of a new column in one batch.
        migrationBuilder.Sql("ALTER TABLE [Departments] ADD [DepartmentId] uniqueidentifier NULL;");
        migrationBuilder.Sql("UPDATE [Departments] SET [DepartmentId] = NEWID();");
        migrationBuilder.Sql("ALTER TABLE [Departments] ALTER COLUMN [DepartmentId] uniqueidentifier NOT NULL;");

        // 2) Switch Department PK to DepartmentId; keep DepartmentCode unique
        migrationBuilder.DropPrimaryKey(
            name: "PK_Departments",
            table: "Departments");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Departments",
            table: "Departments",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Departments_DepartmentCode",
            table: "Departments",
            column: "DepartmentCode",
            unique: true);

        // 3) WorkforceMembers.DepartmentId from DepartmentCode; fail-closed on orphans
        migrationBuilder.Sql("ALTER TABLE [WorkforceMembers] ADD [DepartmentId] uniqueidentifier NULL;");
        migrationBuilder.Sql(
            """
            UPDATE wm
            SET wm.[DepartmentId] = d.[DepartmentId]
            FROM [WorkforceMembers] wm
            INNER JOIN [Departments] d ON d.[DepartmentCode] = wm.[DepartmentCode];
            """);
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM [WorkforceMembers] WHERE [DepartmentId] IS NULL)
            BEGIN
                THROW 50001, 'Migration stopped: WorkforceMembers row(s) have no matching Department after DepartmentId backfill.', 1;
            END
            """);
        migrationBuilder.Sql(
            "ALTER TABLE [WorkforceMembers] ALTER COLUMN [DepartmentId] uniqueidentifier NOT NULL;");

        migrationBuilder.DropColumn(
            name: "DepartmentCode",
            table: "WorkforceMembers");

        migrationBuilder.CreateIndex(
            name: "IX_WorkforceMembers_DepartmentId",
            table: "WorkforceMembers",
            column: "DepartmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_WorkforceMembers_Departments_DepartmentId",
            table: "WorkforceMembers",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "DepartmentId",
            onDelete: ReferentialAction.Restrict);

        // 4) WorkAssignment FKs — fail-closed on orphans
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [WorkAssignments] wa
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [WorkforceMembers] wm
                    WHERE wm.[WorkforceMemberCode] = wa.[WorkforceMemberCode]))
            BEGIN
                THROW 50002, 'Migration stopped: WorkAssignments reference missing WorkforceMemberCode values.', 1;
            END

            IF EXISTS (
                SELECT 1
                FROM [WorkAssignments] wa
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Rooms] r
                    WHERE r.[RoomCode] = wa.[RoomCode]))
            BEGIN
                THROW 50003, 'Migration stopped: WorkAssignments reference missing RoomCode values.', 1;
            END
            """);

        migrationBuilder.CreateIndex(
            name: "IX_WorkAssignments_RoomCode",
            table: "WorkAssignments",
            column: "RoomCode");

        migrationBuilder.AddForeignKey(
            name: "FK_WorkAssignments_WorkforceMembers_WorkforceMemberCode",
            table: "WorkAssignments",
            column: "WorkforceMemberCode",
            principalTable: "WorkforceMembers",
            principalColumn: "WorkforceMemberCode",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkAssignments_Rooms_RoomCode",
            table: "WorkAssignments",
            column: "RoomCode",
            principalTable: "Rooms",
            principalColumn: "RoomCode",
            onDelete: ReferentialAction.Restrict);

        // 5) Loan → Party FK — fail-closed on orphans
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [Loans] l
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Parties] p
                    WHERE p.[PartyCode] = l.[BorrowerPartyReference]))
            BEGIN
                THROW 50004, 'Migration stopped: Loans.BorrowerPartyReference values missing from Parties.', 1;
            END
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Loans_BorrowerPartyReference",
            table: "Loans",
            column: "BorrowerPartyReference");

        migrationBuilder.AddForeignKey(
            name: "FK_Loans_Parties_BorrowerPartyReference",
            table: "Loans",
            column: "BorrowerPartyReference",
            principalTable: "Parties",
            principalColumn: "PartyCode",
            onDelete: ReferentialAction.Restrict);

        // 6) Loan justification columns (nullable for legacy / extract)
        migrationBuilder.AddColumn<string>(
            name: "JustificationKind",
            table: "Loans",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "JustificationDepartmentId",
            table: "Loans",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "JustificationDepartmentCodeSnapshot",
            table: "Loans",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "JustificationRoomCode",
            table: "Loans",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        // 7) ONE-TIME provenance extract from OperatorAudit (C#)
        KeyIssuedJustificationProvenanceExtract.Apply(migrationBuilder);

        // Optional FKs for justification snapshots + indexes
        migrationBuilder.CreateIndex(
            name: "IX_Loans_JustificationDepartmentId",
            table: "Loans",
            column: "JustificationDepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Loans_JustificationRoomCode",
            table: "Loans",
            column: "JustificationRoomCode");

        migrationBuilder.AddForeignKey(
            name: "FK_Loans_Departments_JustificationDepartmentId",
            table: "Loans",
            column: "JustificationDepartmentId",
            principalTable: "Departments",
            principalColumn: "DepartmentId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Loans_Rooms_JustificationRoomCode",
            table: "Loans",
            column: "JustificationRoomCode",
            principalTable: "Rooms",
            principalColumn: "RoomCode",
            onDelete: ReferentialAction.Restrict);

        // 8) CHECK constraint for justification combinations
        migrationBuilder.AddCheckConstraint(
            name: "CK_Loans_Justification",
            table: "Loans",
            sql: """
                (
                    [JustificationKind] IS NULL
                    AND [JustificationDepartmentId] IS NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NULL
                    AND [JustificationRoomCode] IS NULL
                )
                OR
                (
                    [JustificationKind] = N'Department'
                    AND [JustificationDepartmentId] IS NOT NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NOT NULL
                    AND LTRIM(RTRIM([JustificationDepartmentCodeSnapshot])) <> N''
                    AND [JustificationRoomCode] IS NULL
                )
                OR
                (
                    [JustificationKind] = N'Room'
                    AND [JustificationRoomCode] IS NOT NULL
                    AND LTRIM(RTRIM([JustificationRoomCode])) <> N''
                    AND [JustificationDepartmentId] IS NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NULL
                )
                """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "DepartmentIdentityNormalization cannot be reversed. Restore from backup if rollback is required.");
    }
}

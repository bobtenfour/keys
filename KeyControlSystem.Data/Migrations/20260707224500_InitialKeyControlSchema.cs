using Microsoft.EntityFrameworkCore.Migrations;

namespace KeyControlSystem.Data.Migrations;

public partial class InitialKeyControlSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ExecuteSqlScript(migrationBuilder, "sql/01_KeyControlSystemSchema.sql");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS dbo.Runbook;
            DROP TABLE IF EXISTS dbo.RunbookCategory;
            DROP TABLE IF EXISTS dbo.SystemHealthCheck;
            DROP TABLE IF EXISTS dbo.HealthStatus;
            DROP TABLE IF EXISTS dbo.HealthCheckType;
            DROP TABLE IF EXISTS dbo.OutboxMessage;
            DROP TABLE IF EXISTS dbo.IntegrationEndpoint;
            DROP TABLE IF EXISTS dbo.IntegrationEndpointType;
            DROP TABLE IF EXISTS dbo.DashboardDefinition;
            DROP TABLE IF EXISTS dbo.ReportDefinition;
            DROP TABLE IF EXISTS dbo.ReportCategory;
            DROP TABLE IF EXISTS dbo.KpiSnapshot;
            DROP TABLE IF EXISTS dbo.KpiDefinition;
            DROP TABLE IF EXISTS dbo.KpiCategory;
            DROP TABLE IF EXISTS dbo.EventSignature;
            DROP TABLE IF EXISTS dbo.SignatureVerificationStatus;
            DROP TABLE IF EXISTS dbo.PartyCredential;
            DROP TABLE IF EXISTS dbo.SignatureMethod;
            DROP TABLE IF EXISTS dbo.PolicyAction;
            DROP TABLE IF EXISTS dbo.PolicyActionType;
            DROP TABLE IF EXISTS dbo.PolicyCondition;
            DROP TABLE IF EXISTS dbo.PolicyConditionGroup;
            DROP TABLE IF EXISTS dbo.ComparisonOperator;
            DROP TABLE IF EXISTS dbo.ConditionAttribute;
            DROP TABLE IF EXISTS dbo.ConditionValueType;
            DROP TABLE IF EXISTS dbo.LogicalOperator;
            DROP TABLE IF EXISTS dbo.DeviceEvent;
            DROP TABLE IF EXISTS dbo.DeviceEventType;
            DROP TABLE IF EXISTS dbo.Destruction;
            DROP TABLE IF EXISTS dbo.DestructionMethod;
            DROP TABLE IF EXISTS dbo.Retirement;
            DROP TABLE IF EXISTS dbo.DuplicateCreation;
            DROP TABLE IF EXISTS dbo.RekeyAction;
            DROP TABLE IF EXISTS dbo.CylinderReplacement;
            DROP TABLE IF EXISTS dbo.MaintenanceExecution;
            DROP TABLE IF EXISTS dbo.MaintenanceRequest;
            DROP TABLE IF EXISTS dbo.MaintenanceOutcome;
            DROP TABLE IF EXISTS dbo.MaintenancePriority;
            DROP TABLE IF EXISTS dbo.MaintenanceType;
            DROP TABLE IF EXISTS dbo.Investigation;
            DROP TABLE IF EXISTS dbo.InvestigationOutcome;
            DROP TABLE IF EXISTS dbo.InventoryDiscrepancy;
            DROP TABLE IF EXISTS dbo.InventoryDiscrepancyType;
            DROP TABLE IF EXISTS dbo.InventoryCount;
            DROP TABLE IF EXISTS dbo.InventoryCountResult;
            DROP TABLE IF EXISTS dbo.InventoryScope;
            DROP TABLE IF EXISTS dbo.InventorySession;
            DROP TABLE IF EXISTS dbo.InventorySessionType;
            DROP TABLE IF EXISTS dbo.Notification;
            DROP TABLE IF EXISTS dbo.NotificationStatus;
            DROP TABLE IF EXISTS dbo.NotificationTemplate;
            DROP TABLE IF EXISTS dbo.NotificationType;
            DROP TABLE IF EXISTS dbo.PartyContactMethod;
            DROP TABLE IF EXISTS dbo.ContactMethodType;
            DROP TABLE IF EXISTS dbo.Alert;
            DROP TABLE IF EXISTS dbo.AlertRule;
            DROP TABLE IF EXISTS dbo.AlertSeverity;
            DROP TABLE IF EXISTS dbo.AlertType;
            DROP TABLE IF EXISTS dbo.CalendarException;
            DROP TABLE IF EXISTS dbo.CalendarExceptionType;
            DROP TABLE IF EXISTS dbo.BusinessHoursRule;
            DROP TABLE IF EXISTS dbo.BusinessCalendar;
            DROP TABLE IF EXISTS dbo.EmergencyOverride;
            DROP TABLE IF EXISTS dbo.EscalationRule;
            DROP TABLE IF EXISTS dbo.AuthorizationProjection;
            DROP TABLE IF EXISTS dbo.AuthorizationState;
            DROP TABLE IF EXISTS dbo.ApprovalDecision;
            DROP TABLE IF EXISTS dbo.ApprovalDecisionType;
            DROP TABLE IF EXISTS dbo.ApprovalRequirement;
            DROP TABLE IF EXISTS dbo.ApprovalRequirementType;
            DROP TABLE IF EXISTS dbo.AuthorizationRequest;
            DROP TABLE IF EXISTS dbo.PolicyEvaluationPolicy;
            DROP TABLE IF EXISTS dbo.PolicyEvaluation;
            DROP TABLE IF EXISTS dbo.PolicyEvaluationResult;
            DROP TABLE IF EXISTS dbo.Policy;
            DROP TABLE IF EXISTS dbo.PolicyType;
            DROP TABLE IF EXISTS dbo.AuthorizationPurpose;
            DROP TABLE IF EXISTS dbo.KeyCustodyProjection;
            DROP TABLE IF EXISTS dbo.CustodyEvent;
            DROP TABLE IF EXISTS dbo.CustodyTransferReason;
            DROP TABLE IF EXISTS dbo.StorageLocation;
            DROP TABLE IF EXISTS dbo.StorageLocationType;
            DROP TABLE IF EXISTS dbo.StorageSlot;
            DROP TABLE IF EXISTS dbo.RfidCabinetProfile;
            DROP TABLE IF EXISTS dbo.LockerProfile;
            DROP TABLE IF EXISTS dbo.CabinetProfile;
            DROP TABLE IF EXISTS dbo.StorageDevice;
            DROP TABLE IF EXISTS dbo.StorageDeviceType;
            DROP TABLE IF EXISTS dbo.LoanProjection;
            DROP TABLE IF EXISTS dbo.LoanTerm;
            DROP TABLE IF EXISTS dbo.Loan;
            DROP TABLE IF EXISTS dbo.LoanState;
            DROP TABLE IF EXISTS dbo.KeyLifecycleProjection;
            DROP TABLE IF EXISTS dbo.LifecycleTransition;
            DROP TABLE IF EXISTS dbo.LifecycleState;
            DROP TABLE IF EXISTS dbo.AuditRecord;
            DROP TABLE IF EXISTS dbo.AuditActionType;
            DROP TABLE IF EXISTS dbo.EntityType;
            DROP TABLE IF EXISTS dbo.KeyGroupMember;
            DROP TABLE IF EXISTS dbo.KeyGroup;
            DROP TABLE IF EXISTS dbo.KeyAreaAccess;
            DROP TABLE IF EXISTS dbo.AccessLevel;
            DROP TABLE IF EXISTS dbo.KeyAsset;
            DROP TABLE IF EXISTS dbo.EventSchema;
            DROP TABLE IF EXISTS dbo.Event;
            DROP TABLE IF EXISTS dbo.EventStream;
            DROP TABLE IF EXISTS dbo.IntegrityAlgorithm;
            DROP TABLE IF EXISTS dbo.EventType;
            DROP TABLE IF EXISTS dbo.AggregateType;
            DROP TABLE IF EXISTS dbo.KeyType;
            DROP TABLE IF EXISTS dbo.PrincipalRoleAssignment;
            DROP TABLE IF EXISTS dbo.AuthorizationScopeType;
            DROP TABLE IF EXISTS dbo.RolePermission;
            DROP TABLE IF EXISTS dbo.Permission;
            DROP TABLE IF EXISTS dbo.Role;
            DROP TABLE IF EXISTS dbo.SecurityPrincipal;
            DROP TABLE IF EXISTS dbo.SecurityPrincipalType;
            DROP TABLE IF EXISTS dbo.PartyDepartmentAssignment;
            DROP TABLE IF EXISTS dbo.Department;
            DROP TABLE IF EXISTS dbo.ExternalCompanyProfile;
            DROP TABLE IF EXISTS dbo.VendorProfile;
            DROP TABLE IF EXISTS dbo.VisitorProfile;
            DROP TABLE IF EXISTS dbo.ContractorProfile;
            DROP TABLE IF EXISTS dbo.EmployeeProfile;
            DROP TABLE IF EXISTS dbo.Party;
            DROP TABLE IF EXISTS dbo.PartyType;
            DROP TABLE IF EXISTS dbo.Area;
            DROP TABLE IF EXISTS dbo.Facility;
            DROP TABLE IF EXISTS dbo.Site;
            DROP TABLE IF EXISTS dbo.RiskLevel;
            DROP TABLE IF EXISTS dbo.Organization;
            """);
    }

    private static void ExecuteSqlScript(MigrationBuilder migrationBuilder, string relativePath)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
            {
                foreach (var batch in SplitSqlBatches(File.ReadAllText(candidate)))
                {
                    migrationBuilder.Sql(batch);
                }

                return;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate SQL migration script '{relativePath}' from '{baseDirectory}'.");
    }

    private static IEnumerable<string> SplitSqlBatches(string sql)
    {
        using var reader = new StringReader(sql);
        var batch = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                var text = string.Join(Environment.NewLine, batch).Trim();
                if (text.Length > 0)
                {
                    yield return text;
                }

                batch.Clear();
                continue;
            }

            if (!line.TrimStart().StartsWith("USE [", StringComparison.OrdinalIgnoreCase))
            {
                batch.Add(line);
            }
        }

        var finalBatch = string.Join(Environment.NewLine, batch).Trim();
        if (finalBatch.Length > 0)
        {
            yield return finalBatch;
        }
    }
}

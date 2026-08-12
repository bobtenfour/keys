using KeyInventory.Application.Readiness;

namespace KeyInventory.Web.Presentation;

/// <summary>
/// Web presentation over Application-owned <see cref="OperationalReadinessSnapshot"/>.
/// Does not evaluate eligibility; maps snapshot signals to contextual Issue Key messaging only.
/// </summary>
public sealed class OperationalReadinessViewModel
{
    public OperationalReadinessViewModel(OperationalReadinessSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public OperationalReadinessSnapshot Snapshot { get; }

    public string NextActionTitle
    {
        get
        {
            if (Snapshot.CanIssueKey)
            {
                return "Issue Key";
            }

            if (!Snapshot.HasDepartment)
            {
                return "Create a department";
            }

            if (!Snapshot.HasRoom)
            {
                return "Create a room";
            }

            if (!Snapshot.HasKeyType)
            {
                return "Create a key type";
            }

            if (!Snapshot.HasWorkforceMember)
            {
                return "Add a workforce member";
            }

            if (!Snapshot.HasWorkAssignment)
            {
                return "Create a work assignment";
            }

            if (!Snapshot.HasKey)
            {
                return "Register a key";
            }

            return "Review Administration and Catalog";
        }
    }

    public string NextActionDescription
    {
        get
        {
            if (Snapshot.CanIssueKey)
            {
                return "Issue Key prerequisites are satisfied.";
            }

            if (!Snapshot.HasDepartment)
            {
                return "A department is required before workforce members can be created.";
            }

            if (!Snapshot.HasRoom)
            {
                return "A room is required before work assignments can be created.";
            }

            if (!Snapshot.HasKeyType)
            {
                return "A key type is required before a KEY # / physical copy can be registered.";
            }

            if (!Snapshot.HasWorkforceMember)
            {
                return "An active workforce member is required before Issue Key.";
            }

            if (!Snapshot.HasWorkAssignment)
            {
                return "A work assignment (member to room) is required before Issue Key.";
            }

            if (!Snapshot.HasKey)
            {
                return "At least one registered physical key copy is required before Issue Key.";
            }

            return "One or more Issue Key prerequisites are missing.";
        }
    }

    public string NextActionPage
    {
        get
        {
            if (Snapshot.CanIssueKey)
            {
                return "/Operations/Issue";
            }

            if (!Snapshot.HasDepartment)
            {
                return "/Administration/Departments/Add";
            }

            if (!Snapshot.HasRoom)
            {
                return "/Administration/Rooms/Add";
            }

            if (!Snapshot.HasKeyType)
            {
                return "/Catalog/KeyTypes";
            }

            if (!Snapshot.HasWorkforceMember)
            {
                return "/Administration/WorkforceMembers/Add";
            }

            if (!Snapshot.HasWorkAssignment)
            {
                return "/Administration/WorkAssignments/Add";
            }

            if (!Snapshot.HasKey)
            {
                return "/Catalog/Register";
            }

            return "/Administration/Index";
        }
    }
}

using KeyInventory.Application.Readiness;

namespace KeyInventory.Web.Presentation;

public sealed class OperationalReadinessViewModel
{
    public OperationalReadinessViewModel(OperationalReadinessSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public OperationalReadinessSnapshot Snapshot { get; }

    public bool IsFirstUse =>
        !Snapshot.HasDepartment
        || !Snapshot.HasRoom
        || !Snapshot.HasKeyType
        || !Snapshot.HasWorkforceMember
        || !Snapshot.HasWorkAssignment
        || !Snapshot.HasKey;

    public string NextActionTitle
    {
        get
        {
            if (Snapshot.CanIssueKey)
            {
                return "Issue your first key";
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

            return "Review setup";
        }
    }

    public string NextActionDescription
    {
        get
        {
            if (Snapshot.CanIssueKey)
            {
                return "Setup prerequisites are satisfied. Issue Key to begin daily custody.";
            }

            if (!Snapshot.HasDepartment)
            {
                return "Departments group workforce members and support department-based issue justification.";
            }

            if (!Snapshot.HasRoom)
            {
                return "Rooms are places workforce members work and keys may open.";
            }

            if (!Snapshot.HasKeyType)
            {
                return "Key types classify physical keys before registration.";
            }

            if (!Snapshot.HasWorkforceMember)
            {
                return "A workforce member represents a person eligible to receive keys.";
            }

            if (!Snapshot.HasWorkAssignment)
            {
                return "Work assignments link a workforce member to a room and are required before Issue Key.";
            }

            if (!Snapshot.HasKey)
            {
                return "Register at least one key in the catalog before issuing.";
            }

            return "Complete remaining setup tasks below.";
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

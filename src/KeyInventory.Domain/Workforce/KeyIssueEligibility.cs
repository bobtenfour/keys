using KeyInventory.Domain.Parties;

namespace KeyInventory.Domain.Workforce;

/// <summary>
/// Pure eligibility evaluation for key issue. Does not mutate Loan, Return, Audit, Custody, or Lifecycle.
/// </summary>
public static class KeyIssueEligibility
{
    /// <summary>
    /// Structural prerequisites for a person to be considered as a key-issue candidate
    /// before a specific Department/Room justification is chosen.
    /// </summary>
    public static void EnsureIssueCandidate(
        WorkforceMember member,
        Party party,
        Department department,
        IReadOnlyCollection<WorkAssignment> activeAssignments)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(department);
        ArgumentNullException.ThrowIfNull(activeAssignments);

        if (member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may receive a key issue.");
        }

        if (!string.Equals(member.PartyCode, party.PartyCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("WorkforceMember must reference the evaluated Party.");
        }

        if (!party.IsActive)
        {
            throw new InvalidOperationException("Party identity for key issue must be active.");
        }

        if (string.IsNullOrWhiteSpace(party.FirstName)
            || string.IsNullOrWhiteSpace(party.LastName)
            || string.IsNullOrWhiteSpace(party.Uin))
        {
            throw new InvalidOperationException("Party must have FirstName, LastName, and UIN for key issue.");
        }

        if (!department.IsActive
            || !string.Equals(department.DepartmentCode, member.DepartmentCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("WorkforceMember Department must be active and assigned.");
        }

        WorkAssignment[] active = activeAssignments.Where(assignment => assignment.IsActive).ToArray();
        if (active.Length == 0)
        {
            throw new InvalidOperationException("At least one active WorkAssignment is required for key issue.");
        }

        if (active.Count(assignment => assignment.IsPrimary) > 1)
        {
            throw new InvalidOperationException("At most one active WorkAssignment may be primary.");
        }
    }

    public static void EnsureEligible(
        WorkforceMember member,
        Party party,
        Department department,
        IReadOnlyCollection<WorkAssignment> activeAssignments,
        KeyIssueJustificationKind justificationKind,
        string justificationCode)
    {
        EnsureIssueCandidate(member, party, department, activeAssignments);

        string normalizedJustification = WorkforceText.Require(justificationCode, nameof(justificationCode));
        WorkAssignment[] active = activeAssignments.Where(assignment => assignment.IsActive).ToArray();

        if (justificationKind is KeyIssueJustificationKind.None)
        {
            throw new ArgumentOutOfRangeException(nameof(justificationKind), "Justification kind is required.");
        }

        switch (justificationKind)
        {
            case KeyIssueJustificationKind.Department:
                if (!string.Equals(normalizedJustification, member.DepartmentCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Key issue justification Department must be the WorkforceMember authorized Department.");
                }

                break;

            case KeyIssueJustificationKind.Room:
                if (!active.Any(assignment =>
                        string.Equals(assignment.RoomCode, normalizedJustification, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "Key issue justification Room must be an assigned active WorkAssignment Room.");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(justificationKind));
        }
    }
}

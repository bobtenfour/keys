using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class WorkforceDomainInvariantTests
{
    private static readonly Guid DepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void PartyRequiresNineDigitUinAndNames()
    {
        Assert.Throws<ArgumentException>(() => new Party("p1", "Ada", "Lovelace", "12345678"));
        Assert.Throws<ArgumentException>(() => new Party("p1", " ", "Lovelace", "123456789"));
        Party party = new("p1", "Ada", "Lovelace", "123456789");
        Assert.Equal("123456789", party.Uin);
    }

    [Fact]
    public void RoomNumberAndDescriptionAreOwnedByRoom()
    {
        Room room = new("r1", "101", "Lab A");
        Assert.Equal("101", room.RoomNumber);
        Assert.Equal("Lab A", room.Description);
    }

    [Fact]
    public void WorkforceMemberOwnsDepartmentRelationshipFields()
    {
        WorkforceMember member = new("wm1", "p1", WorkforceType.Contractor, DepartmentId);
        Assert.Equal(WorkforceType.Contractor, member.WorkforceType);
        Assert.Equal(DepartmentId, member.DepartmentId);
        Assert.Equal(WorkforceMemberStatus.Active, member.Status);
    }

    [Fact]
    public void WorkAssignmentSupportsPrimaryAndEnd()
    {
        WorkAssignment assignment = new("wa1", "wm1", "r1", isPrimary: true);
        Assert.True(assignment.IsPrimary);
        assignment.End();
        Assert.False(assignment.IsActive);
        Assert.False(assignment.IsPrimary);
    }

    [Fact]
    public void EligibilityAcceptsAuthorizedDepartmentAndRejectsTerminated()
    {
        Party party = new("p1", "Ada", "Lovelace", "123456789");
        Department department = new(DepartmentId, "dept");
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, DepartmentId);
        WorkAssignment[] assignments = [new("wa1", "wm1", "r1", isPrimary: true)];

        KeyIssueEligibility.EnsureEligible(
            member,
            party,
            department,
            assignments,
            KeyIssueJustificationKind.Department,
            "dept");

        member.Terminate();
        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                department,
                assignments,
                KeyIssueJustificationKind.Department,
                "dept"));
    }

    [Fact]
    public void EligibilityRejectsMissingAssignmentAndUnauthorizedRoom()
    {
        Party party = new("p1", "Ada", "Lovelace", "123456789");
        Department department = new(DepartmentId, "dept");
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, DepartmentId);

        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                department,
                [],
                KeyIssueJustificationKind.Department,
                "dept"));

        WorkAssignment[] assignments = [new("wa1", "wm1", "r1", isPrimary: false)];
        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                department,
                assignments,
                KeyIssueJustificationKind.Room,
                "r-other"));
    }

    [Fact]
    public void TerminationDoesNotMutateLoanReturnAuditCustodyOrLifecycleTypes()
    {
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, DepartmentId);
        member.Terminate();
        Assert.Equal(WorkforceMemberStatus.Terminated, member.Status);
        Assert.Null(typeof(WorkforceMember).GetMethod("AutoReturn"));
        Assert.Null(typeof(WorkforceMember).GetMethod("MutateLoan"));
    }
}

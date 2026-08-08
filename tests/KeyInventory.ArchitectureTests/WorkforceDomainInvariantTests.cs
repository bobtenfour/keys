using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class WorkforceDomainInvariantTests
{
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
        Building building = new("b1");
        Room room = new("r1", building, "101", "Lab A");
        Assert.Equal("101", room.RoomNumber);
        Assert.Equal("Lab A", room.Description);
    }

    [Fact]
    public void WorkforceMemberRejectsSelfManagerAndOwnsRelationshipFields()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new WorkforceMember("wm1", "p1", WorkforceType.Employee, "org", "dept", "wm1"));

        WorkforceMember member = new("wm1", "p1", WorkforceType.Contractor, "org", "dept", "wm2");
        Assert.Equal(WorkforceType.Contractor, member.WorkforceType);
        Assert.Equal("org", member.OrganizationCode);
        Assert.Equal("dept", member.DepartmentCode);
        Assert.Equal("wm2", member.ResponsibleManagerWorkforceMemberCode);
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
        Organization organization = new("org");
        Department department = new("dept", organization);
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, "org", "dept", "wm2");
        WorkforceMember manager = new("wm2", "p2", WorkforceType.Employee, "org", "dept", "wm1");
        WorkAssignment[] assignments = [new("wa1", "wm1", "r1", isPrimary: true)];

        KeyIssueEligibility.EnsureEligible(
            member,
            party,
            organization,
            department,
            manager,
            assignments,
            KeyIssueJustificationKind.Department,
            "dept");

        member.Terminate();
        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                organization,
                department,
                manager,
                assignments,
                KeyIssueJustificationKind.Department,
                "dept"));
    }

    [Fact]
    public void EligibilityRejectsMissingAssignmentAndUnauthorizedRoom()
    {
        Party party = new("p1", "Ada", "Lovelace", "123456789");
        Organization organization = new("org");
        Department department = new("dept", organization);
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, "org", "dept", "wm2");
        WorkforceMember manager = new("wm2", "p2", WorkforceType.Employee, "org", "dept", "wm1");

        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                organization,
                department,
                manager,
                [],
                KeyIssueJustificationKind.Department,
                "dept"));

        WorkAssignment[] assignments = [new("wa1", "wm1", "r1", isPrimary: false)];
        Assert.Throws<InvalidOperationException>(() =>
            KeyIssueEligibility.EnsureEligible(
                member,
                party,
                organization,
                department,
                manager,
                assignments,
                KeyIssueJustificationKind.Room,
                "r-other"));
    }

    [Fact]
    public void TerminationDoesNotMutateLoanReturnAuditCustodyOrLifecycleTypes()
    {
        WorkforceMember member = new("wm1", "p1", WorkforceType.Employee, "org", "dept", "wm2");
        member.Terminate();
        Assert.Equal(WorkforceMemberStatus.Terminated, member.Status);
        Assert.Null(typeof(WorkforceMember).GetMethod("AutoReturn"));
        Assert.Null(typeof(WorkforceMember).GetMethod("MutateLoan"));
    }
}

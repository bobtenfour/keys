using System.Reflection;
using System.Text.RegularExpressions;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Pages.Administration.WorkforceMembers;
using KeyInventory.Web.Pages.Operations;
using KeyInventory.Web.Presentation;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorUxBoundaryTests
{
    [Fact]
    public void PartyIdentityFormatterUsesOperatorReadableNameAndUin()
    {
        Assert.Equal(
            "Ada Lovelace — UIN 123456789",
            PartyHolderDisplayFormatter.Format("Ada", "Lovelace", "123456789"));
        Assert.DoesNotContain(
            "WorkforceMemberCode",
            PartyHolderDisplayFormatter.Format("Ada", "Lovelace", "123456789"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void IssuePageUsesOperatorHierarchyAndLocalDateControls()
    {
        string view = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        Assert.Contains("KEY #\r\n", view, StringComparison.Ordinal);
        Assert.Contains("MEDECO Key Code\r\n", view, StringComparison.Ordinal);
        Assert.Contains("Key holder", view, StringComparison.Ordinal);
        Assert.Contains("For\r\n", view, StringComparison.Ordinal);
        Assert.Contains("Issued\r\n", view, StringComparison.Ordinal);
        Assert.Contains("Due\r\n", view, StringComparison.Ordinal);
        Assert.Contains("OperatorTimestampFormatter.ToAbsoluteDisplay", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Issued at (UTC)", view, StringComparison.Ordinal);
        Assert.DoesNotContain("yyyy-MM-ddTHH:mm:sszzz", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Justification code", view, StringComparison.Ordinal);
        Assert.Contains("Loan code", view, StringComparison.Ordinal);
        Assert.Contains("issue-key-copy-data", view, StringComparison.Ordinal);
    }

    [Fact]
    public void IssuePageModelDoesNotExposeRawUtcIsoAsPrimaryBindProperties()
    {
        PropertyInfo[] properties = typeof(IssueModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.IssuedLocalText));
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.DueLocalText));
        Assert.DoesNotContain(properties, property => property.Name == "IssuedAtUtcText");
        Assert.DoesNotContain(properties, property => property.Name == "DueAtUtcText");
        Assert.DoesNotContain(properties, property => property.Name == "IssueReference");
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.LoanCode));
    }

    [Fact]
    public void IssuePageDoesNotReferenceDbContextOrEligibilityType()
    {
        string code = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs");
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyIssueEligibility", code, StringComparison.Ordinal);
        Assert.Contains("ISearchEligibleKeyHoldersUseCase", code, StringComparison.Ordinal);
        Assert.Contains("IGetKeyHolderIssueOptionsUseCase", code, StringComparison.Ordinal);
        Assert.Contains("OperatorLocalTimestamp", code, StringComparison.Ordinal);
    }

    [Fact]
    public void OperatorLocalTimestampPreservesUtcAuthority()
    {
        DateTimeOffset sampleUtc = new(2026, 8, 10, 15, 30, 0, TimeSpan.Zero);
        string control = OperatorLocalTimestamp.ToControlValue(sampleUtc);
        Assert.DoesNotContain("+00:00", control, StringComparison.Ordinal);
        Assert.True(OperatorLocalTimestamp.TryParseToUtc(control, out DateTimeOffset roundTrip, out string? error), error);
        Assert.Equal(TimeSpan.Zero, roundTrip.Offset);
        Assert.Equal(sampleUtc.UtcDateTime, roundTrip.UtcDateTime);
    }

    [Fact]
    public void WorkforceMembersIndexIsListFirstWithoutFormWallOrCodes()
    {
        string view = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Index.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Index.cshtml.cs");
        Assert.Contains("+ Add workforce member", view, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"./Add\"", view, StringComparison.Ordinal);
        Assert.Contains("<th>Name</th>", view, StringComparison.Ordinal);
        Assert.Contains("<th>UIN</th>", view, StringComparison.Ordinal);
        Assert.Contains(">Edit<", view, StringComparison.Ordinal);
        Assert.Contains("Issued Keys", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Create party", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Terminate workforce member", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<th>Party", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<th>Workforce", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Responsible Manager", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin-task-grid", view, StringComparison.Ordinal);
        Assert.Contains("IConfigurationLifecycleUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ICreatePartyUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ICreateWorkforceMemberUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IRegisterWorkforceMemberUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
    }

    [Fact]
    public void AddWorkforceMemberIsDedicatedRouteWithoutOperatorCodes()
    {
        string view = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Add.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Add.cshtml.cs");
        Assert.Contains("First name", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Last name", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UIN", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Party code", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Workforce member code", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Responsible Manager", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("asp-for=\"PartyCode\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-for=\"WorkforceMemberCode\"", view, StringComparison.Ordinal);
        Assert.Contains("IRegisterWorkforceMemberUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IRegisterBootstrapWorkforcePairUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ICreatePartyUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ICreateWorkforceMemberUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);

        ParameterInfo[] registerParameters = typeof(IRegisterWorkforceMemberUseCase)
            .GetMethod(nameof(IRegisterWorkforceMemberUseCase.ExecuteAsync))!
            .GetParameters();
        Assert.DoesNotContain(registerParameters, parameter => parameter.Name is "partyCode" or "workforceMemberCode");
        Assert.Contains(registerParameters, parameter => parameter.Name == "firstName");
        Assert.Contains(registerParameters, parameter => parameter.Name == "uin");
        Assert.Equal(6, registerParameters.Length);
    }

    [Fact]
    public void WorkforceMemberDetailIsDedicatedRouteWithTermination()
    {
        string view = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml.cs");
        Assert.Contains("Terminate workforce member", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConfirmTerminate", view, StringComparison.Ordinal);
        Assert.Contains("Work Assignments / Rooms", view, StringComparison.Ordinal);
        Assert.Contains("Currently issued keys", view, StringComparison.Ordinal);
        Assert.Contains("ITerminateWorkforceMemberUseCase", code, StringComparison.Ordinal);
        Assert.Contains("IUpdateWorkforceMemberDepartmentUseCase", code, StringComparison.Ordinal);
        Assert.Contains("IOperationalKeyLookupUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
        Assert.Equal("KeyInventory.Web.Pages.Administration.WorkforceMembers", typeof(DetailsModel).Namespace);
        Assert.Equal("KeyInventory.Web.Pages.Administration.WorkforceMembers", typeof(AddModel).Namespace);
        Assert.Equal("KeyInventory.Web.Pages.Administration.WorkforceMembers", typeof(IndexModel).Namespace);
    }

    [Fact]
    public void AdministrationListFirstUsesDedicatedAddPagesWithoutOrganizations()
    {
        Assert.False(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Organizations/Add.cshtml")));
        Assert.False(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Organizations/Index.cshtml")));
        Assert.False(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Buildings/Add.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Departments/Add.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/Rooms/Add.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml")));
        string departmentsIndex = Read("src/KeyInventory.Web/Pages/Administration/Departments/Index.cshtml");
        Assert.Contains("+ Add department", departmentsIndex, StringComparison.Ordinal);
        Assert.Contains("Capabilities.CanEdit", departmentsIndex, StringComparison.Ordinal);
        Assert.Contains(">Edit<", departmentsIndex, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedIdentityCodesUseStableOpaquePrefixes()
    {
        string party = WorkforceIdentityCodes.NewPartyCode();
        string member = WorkforceIdentityCodes.NewWorkforceMemberCode();
        Assert.StartsWith("PARTY-", party, StringComparison.Ordinal);
        Assert.StartsWith("WM-", member, StringComparison.Ordinal);
        Assert.NotEqual(party, WorkforceIdentityCodes.NewPartyCode());
        Assert.NotEqual(member, WorkforceIdentityCodes.NewWorkforceMemberCode());
        Assert.True(Guid.TryParse(party["PARTY-".Length..], out _));
        Assert.True(Guid.TryParse(member["WM-".Length..], out _));
    }

    [Fact]
    public void WorkforceMembersPageModelIsPresentationOnly()
    {
        string code = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Index.cshtml.cs");
        Assert.DoesNotContain("EnsureEligible", code, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepoRoot(), relativePath));
    }

    private static string RepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}

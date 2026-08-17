using Xunit;

namespace KeyInventory.ArchitectureTests;

/// <summary>
/// Presentation-boundary tests for the system-wide operator interaction architecture.
/// </summary>
public sealed class OperatorInteractionArchitectureTests
{
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "KeyInventory.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    [Fact]
    public void GlobalSearchPageDoesNotDuplicateHeaderSearchForm()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Search.cshtml"));
        Assert.Contains("Search Results", page, StringComparison.Ordinal);
        Assert.Contains("Search results for", page, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-page=\"/Search\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"q\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain(">Search</button>", page, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonSearchResultsDoNotUseNavigationAsInformationSubstitute()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Search.cshtml"));
        Assert.Contains("Room Assignment", page, StringComparison.Ordinal);
        Assert.Contains("Current Key Custody", page, StringComparison.Ordinal);
        Assert.Contains("No keys currently issued.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member details", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member keys", page, StringComparison.Ordinal);
        Assert.DoesNotContain("View details", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("View keys", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterKeyExposesExactlyTwoModes()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml"));
        string code = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml.cs"));
        Assert.Contains("New Key", page, StringComparison.Ordinal);
        Assert.Contains("Replace Lost Key", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Add New Key", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Create New KEY #", page, StringComparison.Ordinal);
        Assert.DoesNotContain(">Add Key<", page, StringComparison.Ordinal);
        Assert.Contains("ModeNew", code, StringComparison.Ordinal);
        Assert.Contains("ModeReplace", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ModeAdd", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ModeCreate", code, StringComparison.Ordinal);
        Assert.Contains("\"Create Key\"", code, StringComparison.Ordinal);
        Assert.Contains("\"Replace Key\"", code, StringComparison.Ordinal);
        Assert.Contains("RegisterNewKeyAsync", code, StringComparison.Ordinal);
        Assert.Contains("ResolveKeyNumber", code, StringComparison.Ordinal);
        Assert.Contains("IReplaceLostKeyUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterPhysicalCopyUnderExistingKeyNumberAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateNewKeyNumberWithFirstPhysicalCopyAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Register physical copy", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MEDECO Key Code", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("physical MEDECO copy", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IKeyAccessPatternRoomAssignmentUseCase", code, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateKeyAssetOwnsSingleNewKeyOperationWithoutKeyType()
    {
        string useCase = File.ReadAllText(
            Path.Combine(RepoRoot(), "src/KeyInventory.Application/Workflow/CreateKeyAssetUseCase.cs"));
        Assert.Contains("RegisterNewKeyAsync", useCase, StringComparison.Ordinal);
        Assert.Contains("AddNewKeyNumberWithFirstKeyAsync", useCase, StringComparison.Ordinal);
        Assert.Contains("KeyAccessClassification", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateNewKeyNumberWithFirstPhysicalCopyAsync", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterPhysicalCopyUnderExistingKeyNumberAsync", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyType", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireExistingActiveKeyTypeAsync", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("AddKeyTypeAsync", useCase, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiveUsesBoundedActiveIssueSearchNotFullDropdown()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Receive.cshtml"));
        string code = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Receive.cshtml.cs"));
        Assert.Contains("Search KEY #, MEDECO, holder name, or UIN...", page, StringComparison.Ordinal);
        Assert.Contains("ISearchOpenCustodyUseCase", code, StringComparison.Ordinal);
        Assert.Contains("OnGetSearchOpenCustodyAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-items=\"Model.ActiveIssueOptions\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Select an issued key...", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOpenLoansWithHoldersAsync", code, StringComparison.Ordinal);
    }

    [Fact]
    public void IssueUsesBoundedKeyCopySearchNotFullKeyDropdown()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Issue.cshtml"));
        string code = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs"));
        Assert.Contains("Search KEY # or MEDECO", page, StringComparison.Ordinal);
        Assert.Contains("ISearchIssuablePhysicalCopiesUseCase", code, StringComparison.Ordinal);
        Assert.Contains("OnGetSearchIssuableCopiesAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-items=\"Model.KeyNumberOptions\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("issue-key-copy-data", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Select a KEY #...", page, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkAssignmentAddUsesBoundedMemberAndRoomSearch()
    {
        string page = File.ReadAllText(
            Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml"));
        string code = File.ReadAllText(
            Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml.cs"));
        Assert.Contains("SearchActiveWorkforceMembers", code, StringComparison.Ordinal);
        Assert.Contains("SearchActiveRooms", code, StringComparison.Ordinal);
        Assert.DoesNotContain("WorkAssignmentCode", page, StringComparison.Ordinal);
        Assert.DoesNotContain("IsPrimary", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Primary assignment", page, StringComparison.Ordinal);
        Assert.Contains("wa-member-context", page, StringComparison.Ordinal);
    }
}

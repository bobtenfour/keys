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
        Assert.Contains("Work Assignment", page, StringComparison.Ordinal);
        Assert.Contains("Current Key Custody", page, StringComparison.Ordinal);
        Assert.Contains("No keys currently issued.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member details", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member keys", page, StringComparison.Ordinal);
        Assert.DoesNotContain("View details", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("View keys", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterKeyDistinguishesExistingAndNewKeyNumberPaths()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml"));
        string code = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml.cs"));
        Assert.Contains("Register copy under existing KEY #", page, StringComparison.Ordinal);
        Assert.Contains("Create new KEY #", page, StringComparison.Ordinal);
        Assert.Contains("MEDECO Key Code", page, StringComparison.Ordinal);
        Assert.Contains("Enter the MEDECO code printed on this physical key copy.", page, StringComparison.Ordinal);
        Assert.Contains("RegisterPhysicalCopyUnderExistingKeyNumberAsync", code, StringComparison.Ordinal);
        Assert.Contains("CreateNewKeyNumberWithFirstPhysicalCopyAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("New types are created when needed.", page, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateKeyAssetDoesNotSilentlyCreateKeyType()
    {
        string useCase = File.ReadAllText(
            Path.Combine(RepoRoot(), "src/KeyInventory.Application/Workflow/CreateKeyAssetUseCase.cs"));
        Assert.Contains("RequireExistingActiveKeyTypeAsync", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("new KeyType(typeCode)", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("AddKeyTypeAsync", useCase, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiveUsesBoundedActiveIssueSearchNotFullDropdown()
    {
        string page = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Receive.cshtml"));
        string code = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Operations/Receive.cshtml.cs"));
        Assert.Contains("Search active issue", page, StringComparison.Ordinal);
        Assert.Contains("SearchOpenLoansWithHoldersAsync", code, StringComparison.Ordinal);
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
        Assert.Contains("ISearchAvailableKeyCopiesUseCase", code, StringComparison.Ordinal);
        Assert.Contains("SearchKeyCopies", code, StringComparison.Ordinal);
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
    }
}

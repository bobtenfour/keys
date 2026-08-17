using System.Reflection;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Pages.Operations;
using KeyInventory.Web.Presentation;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class IssueReceiveInteractionBoundaryTests
{
    [Fact]
    public void IssueInitialStateHasNoBusinessDefaultsOrFullWorkforceLoad()
    {
        string view = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs");

        Assert.Contains("Key holder", view, StringComparison.Ordinal);
        Assert.Contains("Search by name or UIN", view, StringComparison.Ordinal);
        Assert.Contains("Search KEY # or MEDECO", view, StringComparison.Ordinal);
        Assert.Contains("Search KEY # or MEDECO...", view, StringComparison.Ordinal);
        Assert.Contains("Select justification...", view, StringComparison.Ordinal);
        Assert.Contains("ISearchEligibleKeyHoldersUseCase", code, StringComparison.Ordinal);
        Assert.Contains("ISearchIssuablePhysicalCopiesUseCase", code, StringComparison.Ordinal);
        Assert.Contains("ISearchAvailableKeyCopiesUseCase", code, StringComparison.Ordinal);
        Assert.Contains("JustificationKind { get; set; } = string.Empty", code, StringComparison.Ordinal);

        Assert.DoesNotContain("ListActiveWorkforceMembersWithIdentityAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IListWorkforceMembersUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("JustificationKind { get; set; } = \"Department\"", code, StringComparison.Ordinal);
        Assert.DoesNotContain("issue-justification-data", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Select a KEY #...", view, StringComparison.Ordinal);
        Assert.DoesNotContain("issue-key-copy-data", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Issue to", view, StringComparison.Ordinal);
        Assert.DoesNotContain("class KeyHolder", Read("src/KeyInventory.Domain"), StringComparison.Ordinal);
    }

    [Fact]
    public void KeyHolderSearchIsBoundedApplicationOwnedAndNotAutoSelected()
    {
        string useCase = Read("src/KeyInventory.Application/Workforce/SearchEligibleKeyHoldersUseCase.cs");
        string port = Read("src/KeyInventory.Application/Workforce/IWorkforcePersistencePort.cs");
        string adapter = Read("src/KeyInventory.Infrastructure/Workforce/WorkforcePersistenceAdapter.cs");
        string view = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs");

        Assert.Contains("DefaultMaxResults = 25", useCase, StringComparison.Ordinal);
        Assert.Contains("SearchEligibleKeyHoldersAsync", port, StringComparison.Ordinal);
        Assert.Contains(".Take(bound)", adapter, StringComparison.Ordinal);
        Assert.Contains("EnsureIssueCandidate", useCase, StringComparison.Ordinal);
        Assert.Contains("OnGetSearchHoldersAsync", code, StringComparison.Ordinal);
        Assert.Contains("handler=SearchHolders", view, StringComparison.Ordinal);
        Assert.Contains("Nothing is selected automatically", view, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyIssueEligibility", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
        Assert.Equal(25, ISearchEligibleKeyHoldersUseCase.DefaultMaxResults);
    }

    [Fact]
    public void IssueSuccessUsesPrgCleanStateAndFailedValidationRetainsPage()
    {
        string code = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs");

        Assert.Contains("RedirectToPage()", code, StringComparison.Ordinal);
        Assert.Contains("SuccessTempDataKey", code, StringComparison.Ordinal);
        Assert.Contains("return Page();", code, StringComparison.Ordinal);
        Assert.Contains("catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelState.Clear()", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiveInitialStateDoesNotAutoSelectActiveIssue()
    {
        string view = Read("src/KeyInventory.Web/Pages/Operations/Receive.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Operations/Receive.cshtml.cs");

        Assert.Contains("Search KEY #, MEDECO, holder name, or UIN...", view, StringComparison.Ordinal);
        Assert.Contains("Nothing is selected automatically", view, StringComparison.Ordinal);
        Assert.Contains("ISearchOpenCustodyUseCase", code, StringComparison.Ordinal);
        Assert.Contains("OnGetSearchOpenCustodyAsync", code, StringComparison.Ordinal);
        Assert.Contains("FormatKeyCopy", code, StringComparison.Ordinal);
        Assert.Contains("RedirectToPage()", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelState.Clear()", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ActiveIssueOptions[0]", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Select an issued key...", view, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-items=\"Model.ActiveIssueOptions\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedTimestampAuthorityUsesAbsoluteDisplayAndUtcPersistenceBoundary()
    {
        string formatter = Read("src/KeyInventory.Web/Presentation/OperatorTimestampFormatter.cs");
        string local = Read("src/KeyInventory.Web/Presentation/OperatorLocalTimestamp.cs");
        string issueView = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        string receiveView = Read("src/KeyInventory.Web/Pages/Operations/Receive.cshtml");

        Assert.Contains("ToAbsoluteDisplay", formatter, StringComparison.Ordinal);
        Assert.Contains("MMM d, yyyy · h:mm tt", formatter, StringComparison.Ordinal);
        Assert.Contains("ToOperatorEntryValue", local, StringComparison.Ordinal);
        Assert.Contains("OperatorTimestampFormatter.ToAbsoluteDisplay", issueView, StringComparison.Ordinal);
        Assert.Contains("OperatorTimestampFormatter.ToAbsoluteDisplay", receiveView, StringComparison.Ordinal);
        Assert.DoesNotContain("datetime-local", issueView, StringComparison.Ordinal);
        Assert.DoesNotContain("datetime-local", receiveView, StringComparison.Ordinal);

        DateTimeOffset sample = new(2026, 8, 11, 22, 14, 0, TimeSpan.Zero);
        string absolute = OperatorTimestampFormatter.ToAbsoluteDisplay(sample);
        Assert.Contains("2026", absolute, StringComparison.Ordinal);
        Assert.Contains("·", absolute, StringComparison.Ordinal);
        Assert.True(OperatorLocalTimestamp.TryParseToUtc(absolute, out DateTimeOffset utc, out string? error), error);
        Assert.Equal(TimeSpan.Zero, utc.Offset);
    }

    [Fact]
    public void IssuePageModelKeepsOperatorEditableIssuedAndDueBindProperties()
    {
        PropertyInfo[] properties = typeof(IssueModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.IssuedLocalText));
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.DueLocalText));
        Assert.Contains(properties, property => property.Name == nameof(IssueModel.SelectedHolderDisplay));
        Assert.DoesNotContain(properties, property => property.Name == "IssuedAtUtcText");
        Assert.DoesNotContain(properties, property => property.Name == "WorkforceMemberOptions");
    }

    private static string Read(string relativePath)
    {
        string fullPath = Path.Combine(RepoRoot(), relativePath);
        if (Directory.Exists(fullPath))
        {
            return string.Concat(
                Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));
        }

        return File.ReadAllText(fullPath);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "KeyInventory.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("KeyInventory.sln was not found.");
    }
}

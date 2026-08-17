using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorExperiencePresentationBoundaryTests
{
    [Fact]
    public void HomeIsOperationalDashboardWithoutPermanentSetupSurfaces()
    {
        string view = Read("src/KeyInventory.Web/Pages/Index.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Index.cshtml.cs");

        Assert.Contains("Active Loans", view, StringComparison.Ordinal);
        Assert.Contains("Overdue", view, StringComparison.Ordinal);
        Assert.Contains("Keys Available", view, StringComparison.Ordinal);
        Assert.Contains("Daily custody", view, StringComparison.Ordinal);
        Assert.Contains("Recent Activity", view, StringComparison.Ordinal);

        Assert.DoesNotContain("First-time setup", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Setup readiness", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("_OperationalReadiness", view, StringComparison.Ordinal);
        Assert.DoesNotContain("readiness-checklist", view, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReadinessUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalReadinessViewModel", code, StringComparison.Ordinal);
    }

    [Fact]
    public void AdministrationHubIsOrdinaryCapabilitiesNotOnboarding()
    {
        string view = Read("src/KeyInventory.Web/Pages/Administration/Index.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Administration/Index.cshtml.cs");

        Assert.Contains("Departments", view, StringComparison.Ordinal);
        Assert.Contains("Rooms", view, StringComparison.Ordinal);
        Assert.Contains("Workforce Members", view, StringComparison.Ordinal);
        Assert.Contains("Room Assignments", view, StringComparison.Ordinal);
        Assert.Contains("Audit Trail", view, StringComparison.Ordinal);

        Assert.DoesNotContain("_OperationalReadiness", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Setup readiness", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Setup tasks", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Parallel first steps", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("First-time setup", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IOperationalReadinessUseCase", code, StringComparison.Ordinal);
    }

    [Fact]
    public void IssueKeyPresentsContextualPrerequisitesFromApplicationSignalsOnly()
    {
        string view = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        string code = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs");
        string partial = Read("src/KeyInventory.Web/Pages/Shared/_OperationalReadiness.cshtml");
        string vm = Read("src/KeyInventory.Web/Presentation/OperationalReadinessViewModel.cs");

        Assert.Contains("_OperationalReadiness", view, StringComparison.Ordinal);
        Assert.Contains("!Model.Readiness.Snapshot.CanIssueKey", view, StringComparison.Ordinal);
        Assert.Contains("IOperationalReadinessUseCase", code, StringComparison.Ordinal);
        Assert.Contains("Issue Key unavailable", partial, StringComparison.Ordinal);
        Assert.Contains("@if (!s.CanIssueKey)", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("Setup readiness", partial, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("First-time setup", partial, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KEY #↔Room assignments", partial, StringComparison.Ordinal);
        Assert.Contains("Does not evaluate eligibility", vm, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyIssueEligibility", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductExperienceContractForbidsPermanentHomeOnboarding()
    {
        string contract = Read("documentation/product-experience-contract.md");

        Assert.Contains("permanent first-time setup section", contract, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Home is an operational dashboard", contract, StringComparison.Ordinal);
        Assert.Contains("contextually at that capability boundary", contract, StringComparison.Ordinal);
        Assert.Contains("Application-owned readiness/eligibility remains the sole business readiness authority", contract, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
        => File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

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

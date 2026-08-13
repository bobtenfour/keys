using System.Reflection;
using KeyInventory.Web.Pages;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class HelpPresentationTests
{
    [Fact]
    public void HelpPageExistsAsPresentationOnlyModel()
    {
        Assert.True(typeof(HelpModel).IsSubclassOf(typeof(Microsoft.AspNetCore.Mvc.RazorPages.PageModel)));
        Assert.Empty(typeof(HelpModel).GetConstructors().Single().GetParameters());
        Assert.DoesNotContain(
            typeof(HelpModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            field => (field.FieldType.FullName ?? field.FieldType.Name).Contains("DbContext", StringComparison.Ordinal)
                || (field.FieldType.Namespace?.StartsWith("KeyInventory.Application", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void HelpMarkupContainsApprovedStructureAndLinks()
    {
        string help = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Help.cshtml"));
        Assert.Contains("@page", help, StringComparison.Ordinal);
        Assert.Contains("help.css", help, StringComparison.Ordinal);
        Assert.Contains("id=\"orient\"", help, StringComparison.Ordinal);
        Assert.Contains("id=\"configure\"", help, StringComparison.Ordinal);
        Assert.Contains("id=\"operate\"", help, StringComparison.Ordinal);
        Assert.Contains("id=\"govern\"", help, StringComparison.Ordinal);
        Assert.Contains("What do you need to do?", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-01", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-02", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-03", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-04", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-05", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-06", help, StringComparison.Ordinal);
        Assert.Contains("HELP-DIAGRAM-07", help, StringComparison.Ordinal);
        Assert.Contains("KEY #", help, StringComparison.Ordinal);
        Assert.Contains("MEDECO", help, StringComparison.Ordinal);
        Assert.Contains("Room #", help, StringComparison.Ordinal);
        Assert.Contains("Which KEY # values open Room", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Operations/Issue\"", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Operations/Receive\"", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Operations/Find\"", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Catalog/KeyRooms\"", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Administration/Departments/Index\"", help, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Reports/Index\"", help, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", help, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject", help, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogKeyCode", help, StringComparison.Ordinal);
        Assert.DoesNotContain("mermaid", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn.", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Organization", help, StringComparison.Ordinal);
        Assert.DoesNotContain("Building", help, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyHolder entity", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("every entity supports", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("images/help/01-catalog-keys.png", help, StringComparison.Ordinal);
        Assert.Contains("images/help/02-issue-key.png", help, StringComparison.Ordinal);
        Assert.Contains("images/help/03-active-loans.png", help, StringComparison.Ordinal);
        Assert.Contains("images/help/04-find-key.png", help, StringComparison.Ordinal);
        Assert.Contains("images/help/05-audit-trail.png", help, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalNavigationIncludesHelpAndHomeHasNoSetupChecklist()
    {
        string layout = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Shared/_Layout.cshtml"));
        Assert.Contains("asp-page=\"/Help\"", layout, StringComparison.Ordinal);
        Assert.Contains(">Help<", layout, StringComparison.Ordinal);

        string composition = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/WebServiceComposition.cs"));
        Assert.Contains("AuthorizePage(\"/Help\")", composition, StringComparison.Ordinal);

        string home = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Index.cshtml"));
        Assert.DoesNotContain("First-time setup", home, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("setup checklist", home, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onboarding", home, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelpScreenshotAssetsExist()
    {
        string dir = Path.Combine(RepoRoot(), "src/KeyInventory.Web/wwwroot/images/help");
        string[] required =
        [
            "01-catalog-keys.png",
            "02-issue-key.png",
            "03-active-loans.png",
            "04-find-key.png",
            "05-audit-trail.png"
        ];
        foreach (string file in required)
        {
            string path = Path.Combine(dir, file);
            Assert.True(File.Exists(path), $"Missing Help screenshot: {file}");
            Assert.True(new FileInfo(path).Length > 1024, $"Help screenshot too small: {file}");
        }
    }

    [Fact]
    public void HelpStylesheetExistsWithoutExternalDiagramLibraries()
    {
        string css = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/wwwroot/css/help.css"));
        Assert.Contains(".help-page", css, StringComparison.Ordinal);
        Assert.Contains(".help-diagram-frame", css, StringComparison.Ordinal);
        Assert.DoesNotContain("mermaid", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", css, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "KeyInventory.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}

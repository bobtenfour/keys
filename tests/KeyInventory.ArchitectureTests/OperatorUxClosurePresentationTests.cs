using Xunit;

namespace KeyInventory.ArchitectureTests;

/// <summary>
/// Structural presentation tests for system-wide operator UX closure.
/// </summary>
public sealed class OperatorUxClosurePresentationTests
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

    private static string Read(string relativePath)
        => File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    [Fact]
    public void WorkforceMemberCreateRedirectsToDetailsViewNotHybridEdit()
    {
        string addCode = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Add.cshtml.cs");
        string detailsView = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml");
        string detailsCode = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml.cs");

        Assert.Contains("RedirectToPage(\"./Details\"", addCode, StringComparison.Ordinal);
        Assert.Contains("JustCreated", addCode, StringComparison.Ordinal);
        Assert.Contains("JustCreated", detailsCode, StringComparison.Ordinal);
        Assert.Contains("IsEditMode", detailsCode, StringComparison.Ordinal);
        Assert.Contains("asp-route-edit=\"true\"", detailsView, StringComparison.Ordinal);
        Assert.Contains("<dt>Department</dt>", detailsView, StringComparison.Ordinal);
        Assert.Contains("!justCreated", detailsView, StringComparison.Ordinal);
        Assert.Contains(
            "Terminated",
            Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.DoesNotContain("asp-page-handler=\"Maintain\"", detailsView.Split("justCreated")[0], StringComparison.Ordinal);
    }

    [Fact]
    public void WorkforceMemberDetailsLoadsObligationsOnlyWhenTerminated()
    {
        string detailsCode = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml.cs");
        Assert.Contains(
            "string.Equals(Selected.Status, \"Terminated\", StringComparison.Ordinal)",
            detailsCode,
            StringComparison.Ordinal);
        Assert.Contains("Obligations = [];", detailsCode, StringComparison.Ordinal);
    }

    [Fact]
    public void TerminateIsHiddenOnJustCreatedDetailsSuccessPath()
    {
        string detailsView = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml");
        int lifecycleIndex = detailsView.IndexOf("Lifecycle", StringComparison.Ordinal);
        int justCreatedGuard = detailsView.IndexOf("isActive && !justCreated", StringComparison.Ordinal);
        Assert.True(justCreatedGuard >= 0, "Terminate section must be gated by !justCreated.");
        Assert.True(lifecycleIndex > justCreatedGuard, "Lifecycle/Terminate must be inside the !justCreated guard.");
        Assert.Contains("Terminate workforce member", detailsView, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperatorFacingWorkAssignmentTerminologyIsRemovedFromActiveUi()
    {
        string[] pages =
        [
            "src/KeyInventory.Web/Pages/Shared/_Layout.cshtml",
            "src/KeyInventory.Web/Pages/Administration/Index.cshtml",
            "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Index.cshtml",
            "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml",
            "src/KeyInventory.Web/Pages/Administration/WorkAssignments/Delete.cshtml",
            "src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Details.cshtml",
            "src/KeyInventory.Web/Pages/Search.cshtml",
            "src/KeyInventory.Web/Pages/Operations/Issue.cshtml",
            "src/KeyInventory.Web/Pages/Help.cshtml"
        ];

        foreach (string page in pages)
        {
            string content = Read(page);
            Assert.DoesNotContain("Work Assignment", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Work Assignments", content, StringComparison.Ordinal);
            Assert.DoesNotContain("work assignment", content, StringComparison.Ordinal);
            Assert.DoesNotContain("work assignments", content, StringComparison.Ordinal);
            Assert.DoesNotContain(">Work Assign<", content, StringComparison.Ordinal);
        }

        Assert.Contains("Room Assignments", Read("src/KeyInventory.Web/Pages/Shared/_Layout.cshtml"), StringComparison.Ordinal);
        Assert.Contains("Assign Room", Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml"), StringComparison.Ordinal);
        Assert.Contains(">Assign Room</button>", Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml"), StringComparison.Ordinal);
    }

    [Fact]
    public void RoomAssignmentCreateContainsOnlyMemberAndRoomAndCleansAfterSuccess()
    {
        string addView = Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml");
        string addCode = Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml.cs");

        Assert.Contains("Workforce member", addView, StringComparison.Ordinal);
        Assert.Contains(
            "<label>\n                Room\n",
            addView.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.DoesNotContain("WorkAssignmentCode", addView, StringComparison.Ordinal);
        Assert.DoesNotContain("IsPrimary", addView, StringComparison.Ordinal);
        Assert.DoesNotContain("Primary assignment", addView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RedirectToPage(\"./Add\")", addCode, StringComparison.Ordinal);
        Assert.Contains("WorkforceMemberCode = string.Empty", addCode, StringComparison.Ordinal);
        Assert.Contains("RoomCode = string.Empty", addCode, StringComparison.Ordinal);
        Assert.Contains("Room was assigned.", addCode, StringComparison.Ordinal);
    }

    [Fact]
    public void MutationSurfacesUseExplicitPrgSuccessPatterns()
    {
        Assert.Contains("RedirectToPage(\"./Details\"",
            Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Add.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("RedirectToPage(\"./Add\")",
            Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("RedirectToPage(new { mode = ModeNew })",
            Read("src/KeyInventory.Web/Pages/Catalog/Register.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("return RedirectToPage();",
            Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("return RedirectToPage();",
            Read("src/KeyInventory.Web/Pages/Operations/Receive.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("RedirectToPage(\"./Index\")",
            Read("src/KeyInventory.Web/Pages/Administration/Departments/Add.cshtml.cs"),
            StringComparison.Ordinal);
        Assert.Contains("RedirectToPage(\"./Index\")",
            Read("src/KeyInventory.Web/Pages/Administration/Rooms/Add.cshtml.cs"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterModesAndGlobalSearchGuardsRemainIntact()
    {
        string register = Read("src/KeyInventory.Web/Pages/Catalog/Register.cshtml");
        string registerCode = Read("src/KeyInventory.Web/Pages/Catalog/Register.cshtml.cs");
        string search = Read("src/KeyInventory.Web/Pages/Search.cshtml");

        Assert.Contains("New Key", register, StringComparison.Ordinal);
        Assert.Contains("Replace Lost Key", register, StringComparison.Ordinal);
        Assert.DoesNotContain("Add New Key", register, StringComparison.Ordinal);
        Assert.DoesNotContain("Create New KEY #", register, StringComparison.Ordinal);
        Assert.Contains("ModeNew", registerCode, StringComparison.Ordinal);
        Assert.Contains("ModeReplace", registerCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ModeAdd", registerCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ModeCreate", registerCode, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"q\"", search, StringComparison.Ordinal);
        Assert.DoesNotContain(">Search</button>", search, StringComparison.Ordinal);
        Assert.DoesNotContain("Member details", search, StringComparison.Ordinal);
        Assert.DoesNotContain("Member keys", search, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryNavigationOrdersHomeCatalogThenOperations()
    {
        string layout = Read("src/KeyInventory.Web/Pages/Shared/_Layout.cshtml");
        int primaryStart = layout.IndexOf("aria-label=\"Primary\"", StringComparison.Ordinal);
        Assert.True(primaryStart >= 0, "Primary navigation must exist once.");
        int primaryEnd = layout.IndexOf("</nav>", primaryStart, StringComparison.Ordinal);
        Assert.True(primaryEnd > primaryStart);
        string primary = layout[primaryStart..primaryEnd];

        int home = primary.IndexOf(">Home<", StringComparison.Ordinal);
        int catalog = primary.IndexOf(">Catalog<", StringComparison.Ordinal);
        int operations = primary.IndexOf(">Operations<", StringComparison.Ordinal);
        int reports = primary.IndexOf(">Reports<", StringComparison.Ordinal);
        int administration = primary.IndexOf(">Administration<", StringComparison.Ordinal);
        int help = primary.IndexOf(">Help<", StringComparison.Ordinal);

        Assert.True(home >= 0 && catalog > home && operations > catalog,
            "Primary order must be Home → Catalog → Operations.");
        Assert.True(reports > operations && administration > reports && help > administration,
            "Remaining primary items keep Reports → Administration → Help.");
        Assert.Equal(1, CountOccurrences(primary, "asp-page=\"/Index\""));
        Assert.Equal(1, CountOccurrences(primary, "asp-page=\"/Catalog/Keys\""));
        Assert.Equal(1, CountOccurrences(primary, "asp-page=\"/Operations/Issue\""));
    }

    [Fact]
    public void CreateKeyReplacesRegisterKeyOperatorFacingAndIssueRemainsDistinct()
    {
        string layout = Read("src/KeyInventory.Web/Pages/Shared/_Layout.cshtml");
        string register = Read("src/KeyInventory.Web/Pages/Catalog/Register.cshtml");
        string keys = Read("src/KeyInventory.Web/Pages/Catalog/Keys.cshtml");
        string issue = Read("src/KeyInventory.Web/Pages/Operations/Issue.cshtml");
        string help = Read("src/KeyInventory.Web/Pages/Help.cshtml");
        string readiness = Read("src/KeyInventory.Web/Pages/Shared/_OperationalReadiness.cshtml");
        string product = Read("documentation/product-experience-contract.md");

        Assert.Contains(">Create Key<", layout, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Catalog/Register\"", layout, StringComparison.Ordinal);
        Assert.Contains("<h1>Create Key</h1>", register, StringComparison.Ordinal);
        Assert.Contains("Create Key", keys, StringComparison.Ordinal);
        Assert.Contains("Create Key", issue, StringComparison.Ordinal);
        Assert.Contains("Open Create Key", help, StringComparison.Ordinal);
        Assert.Contains(">Create Key<", readiness, StringComparison.Ordinal);
        Assert.Contains("**Create Key.**", product, StringComparison.Ordinal);

        Assert.DoesNotContain("Register Key", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Register Key", register, StringComparison.Ordinal);
        Assert.DoesNotContain("Register Key", keys, StringComparison.Ordinal);
        Assert.DoesNotContain("Register Key", issue, StringComparison.Ordinal);
        Assert.DoesNotContain("Register Key", help, StringComparison.Ordinal);
        Assert.DoesNotContain("Register Key", readiness, StringComparison.Ordinal);

        Assert.Contains(">Issue Key<", layout, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Operations/Issue\"", layout, StringComparison.Ordinal);
        Assert.Contains("<h1>Issue Key</h1>", issue, StringComparison.Ordinal);
        Assert.Contains("Open Issue Key", help, StringComparison.Ordinal);
        Assert.DoesNotContain("<h1>Issue Key</h1>", register, StringComparison.Ordinal);
    }

    [Fact]
    public void DepartmentAndRoomAssignmentOperatorTerminologyRemainCorrect()
    {
        string deptAdd = Read("src/KeyInventory.Web/Pages/Administration/Departments/Add.cshtml");
        string deptEdit = Read("src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml");
        string layout = Read("src/KeyInventory.Web/Pages/Shared/_Layout.cshtml");
        string workAdd = Read("src/KeyInventory.Web/Pages/Administration/WorkAssignments/Add.cshtml");
        string help = Read("src/KeyInventory.Web/Pages/Help.cshtml");
        string readiness = Read("src/KeyInventory.Web/Pages/Shared/_OperationalReadiness.cshtml");

        Assert.Contains(
            "<label>\n            Department\n",
            deptAdd.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains(
            "<label>\n            Department\n",
            deptEdit.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.DoesNotContain("Department Code", deptAdd, StringComparison.Ordinal);
        Assert.DoesNotContain("Department Code", deptEdit, StringComparison.Ordinal);
        Assert.DoesNotContain("Department code", deptAdd, StringComparison.Ordinal);
        Assert.DoesNotContain("Department code", deptEdit, StringComparison.Ordinal);

        Assert.Contains("Room Assignments", layout, StringComparison.Ordinal);
        Assert.Contains("Assign Room", workAdd, StringComparison.Ordinal);
        Assert.Contains("Room Assignment", help, StringComparison.Ordinal);
        Assert.Contains("Room Assignment", readiness, StringComparison.Ordinal);
        Assert.DoesNotContain("Work Assignment", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Work Assignments", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Work assignment", readiness, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogSubnavExposesCreateKeyWithoutDuplicatingPrimaryNavAuthority()
    {
        string layout = Read("src/KeyInventory.Web/Pages/Shared/_Layout.cshtml");
        int catalogSubnav = layout.IndexOf("aria-label=\"Catalog\"", StringComparison.Ordinal);
        Assert.True(catalogSubnav >= 0);
        int catalogEnd = layout.IndexOf("</nav>", catalogSubnav, StringComparison.Ordinal);
        string catalogNav = layout[catalogSubnav..catalogEnd];
        Assert.Contains(">Keys<", catalogNav, StringComparison.Ordinal);
        Assert.Contains(">Create Key<", catalogNav, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Catalog/Register\"", catalogNav, StringComparison.Ordinal);

        int opsSubnav = layout.IndexOf("aria-label=\"Operations\"", StringComparison.Ordinal);
        Assert.True(opsSubnav >= 0);
        int opsEnd = layout.IndexOf("</nav>", opsSubnav, StringComparison.Ordinal);
        string opsNav = layout[opsSubnav..opsEnd];
        Assert.Contains(">Issue Key<", opsNav, StringComparison.Ordinal);
        Assert.DoesNotContain("Create Key", opsNav, StringComparison.Ordinal);
        Assert.DoesNotContain("/Catalog/Register", opsNav, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    [Fact]
    public void WebPagesDoNotReferenceDbContext()
    {
        string webRoot = Path.Combine(RepoRoot(), "src", "KeyInventory.Web", "Pages");
        foreach (string file in Directory.EnumerateFiles(webRoot, "*.cs*", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("KeyInventoryDbContext", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        }
    }
}

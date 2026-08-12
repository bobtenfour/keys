using System.Text.RegularExpressions;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class DemoResetBoundaryTests
{
    [Fact]
    public void ResetScriptExistsUnderDockerDemoBoundary()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "docker", "reset-keyinventory-demo.sh")));
    }

    [Fact]
    public void ResetScriptHardCodesKeyInventoryDemoAndRejectsArguments()
    {
        string script = Read("docker/reset-keyinventory-demo.sh");

        Assert.Contains("TARGET_DATABASE=\"KeyInventoryDemo\"", script, StringComparison.Ordinal);
        Assert.Contains("DROP DATABASE [KeyInventoryDemo]", script, StringComparison.Ordinal);
        Assert.Contains("This script accepts no arguments", script, StringComparison.Ordinal);
        Assert.Contains("[[ \"${#}\" -ne 0 ]]", script, StringComparison.Ordinal);

        Assert.DoesNotContain("TARGET_DATABASE=\"$1\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain("TARGET_DATABASE=${1", script, StringComparison.Ordinal);
        Assert.DoesNotContain("--database", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE [$1]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [${", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetScriptProtectsForbiddenDatabaseNames()
    {
        string script = Read("docker/reset-keyinventory-demo.sh");

        string[] protectedNames =
        [
            "KeyInventoryDev",
            "DentalInventoryDemo",
            "DentalInventoryDev",
            "master",
            "model",
            "msdb",
            "tempdb"
        ];

        foreach (string name in protectedNames)
        {
            Assert.Contains($"\"{name}\"", script, StringComparison.Ordinal);
        }

        Assert.Contains("FORBIDDEN_DATABASES", script, StringComparison.Ordinal);
        Assert.Contains("Refusing destructive reset: target collides with protected database", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [KeyInventoryDev]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [DentalInventoryDemo]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [master]", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetScriptIsDemoOnlyAndFailClosed()
    {
        string script = Read("docker/reset-keyinventory-demo.sh");

        Assert.Contains("DEMO / EVALUATION ONLY", script, StringComparison.Ordinal);
        Assert.Contains("Reset is Demo-only", script, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_ENVIRONMENT", script, StringComparison.Ordinal);
        Assert.Contains("must be Demo when set", script, StringComparison.Ordinal);
        Assert.Contains("Database=KeyInventoryDemo", script, StringComparison.Ordinal);
        Assert.Contains("MSSQL_SA_PASSWORD is required", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetScriptForbidsVolumeAndSqlContainerDestruction()
    {
        string script = Read("docker/reset-keyinventory-demo.sh");

        Assert.DoesNotContain("docker compose down -v", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker-compose down -v", script, StringComparison.Ordinal);
        Assert.DoesNotContain("volume rm", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker volume", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker rm", script, StringComparison.Ordinal);
        Assert.Contains("Do not create or recreate SQL here", script, StringComparison.Ordinal);
        Assert.Contains("dentalinventory-demo-sql", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalDemoComposeRemainsNonDestructive()
    {
        string compose = Read("docker-compose.demo.yml");

        Assert.Contains("NON-DESTRUCTIVE", compose, StringComparison.Ordinal);
        Assert.Contains("Database=KeyInventoryDemo", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reset-keyinventory-demo", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("volume rm", compose, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("migrate", compose, StringComparison.Ordinal);
        Assert.Contains("web", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrateAndWebEntrypointsDoNotDropDatabases()
    {
        string migrate = Read("docker/migrate-entrypoint.sh");
        string web = Read("docker/web-entrypoint.sh");

        Assert.Contains("dotnet ef database update", migrate, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE", migrate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reset-keyinventory-demo", migrate, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE", web, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reset-keyinventory-demo", web, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyAccessCopy1MigrationStopRemainsUnchanged()
    {
        string migration = Read(
            "src/KeyInventory.Infrastructure/Data/Migrations/20260812015021_KeyAccessCopy1.cs");

        Assert.Contains("THROW 50001", migration, StringComparison.Ordinal);
        Assert.Contains("CatalogKeyCode cannot be mapped", migration, StringComparison.Ordinal);
        Assert.Contains("IF EXISTS (SELECT 1 FROM KeyAssets)", migration, StringComparison.Ordinal);
        Assert.Contains("OR EXISTS (SELECT 1 FROM KeyRoomAssignments)", migration, StringComparison.Ordinal);
        Assert.Contains("OR EXISTS (SELECT 1 FROM Loans)", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void WebContainsNoDemoResetEndpoint()
    {
        string webRoot = Path.Combine(RepoRoot(), "src", "KeyInventory.Web");
        List<string> hits = Directory
            .EnumerateFiles(webRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return text.Contains("reset-keyinventory-demo", StringComparison.OrdinalIgnoreCase)
                    || Regex.IsMatch(text, @"DROP\s+DATABASE", RegexOptions.IgnoreCase);
            })
            .Select(path => Path.GetRelativePath(RepoRoot(), path))
            .ToList();

        Assert.Empty(hits);
    }

    [Fact]
    public void ReadmeSeparatesNormalDeployFromResetReseed()
    {
        string readme = Read("docker/README-demo.md");

        Assert.Contains("## NORMAL DEPLOY", readme, StringComparison.Ordinal);
        Assert.Contains("## RESET / RESEED DEMO", readme, StringComparison.Ordinal);
        Assert.Contains("## COMPLETE CLEAN REBUILD (KeyInventory only)", readme, StringComparison.Ordinal);
        Assert.Contains("Preserves", readme, StringComparison.Ordinal);
        Assert.Contains("docker/reset-keyinventory-demo.sh", readme, StringComparison.Ordinal);
        Assert.Contains("./docker/reset-keyinventory-demo.sh", readme, StringComparison.Ordinal);
        Assert.Contains("run --rm migrate", readme, StringComparison.Ordinal);
        Assert.Contains("Business data is left **empty**", readme, StringComparison.Ordinal);
        Assert.Contains("Do **not** use `docker compose down -v`", readme, StringComparison.Ordinal);
        Assert.Contains("**Preserves** existing `KeyInventoryDemo`", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void EfMigrationsRemainSchemaRecreationAuthorityForDemoResetFlow()
    {
        string script = Read("docker/reset-keyinventory-demo.sh");
        string migrate = Read("docker/migrate-entrypoint.sh");
        string readme = Read("docker/README-demo.md");

        Assert.Contains("Does NOT migrate", script, StringComparison.Ordinal);
        Assert.Contains("dotnet ef database update", migrate, StringComparison.Ordinal);
        Assert.Contains("existing migrate service", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet ef database update", readme, StringComparison.Ordinal);
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

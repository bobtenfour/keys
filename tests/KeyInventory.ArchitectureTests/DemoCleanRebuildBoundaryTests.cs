using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class DemoCleanRebuildBoundaryTests
{
    [Fact]
    public void CleanRebuildScriptExistsUnderDockerDemoBoundary()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "docker", "clean-rebuild-keyinventory-demo.sh")));
    }

    [Fact]
    public void CleanRebuildScriptHardCodesTargetsAndRejectsArguments()
    {
        string script = Read("docker/clean-rebuild-keyinventory-demo.sh");

        Assert.Contains("INSTALL_DIR=\"/opt/keys\"", script, StringComparison.Ordinal);
        Assert.Contains("TARGET_DATABASE=\"KeyInventoryDemo\"", script, StringComparison.Ordinal);
        Assert.Contains("DROP DATABASE [KeyInventoryDemo]", script, StringComparison.Ordinal);
        Assert.Contains("rm -rf \"${INSTALL_DIR}\"", script, StringComparison.Ordinal);
        Assert.Contains("git clone --branch \"${REPO_BRANCH}\"", script, StringComparison.Ordinal);
        Assert.Contains("https://github.com/bobtenfour/keys.git", script, StringComparison.Ordinal);
        Assert.Contains("This script accepts no arguments", script, StringComparison.Ordinal);
        Assert.Contains("[[ \"${#}\" -ne 0 ]]", script, StringComparison.Ordinal);

        Assert.DoesNotContain("TARGET_DATABASE=\"$1\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain("INSTALL_DIR=\"$1\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain("--database", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE [$1]", script, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanRebuildScriptProtectsForbiddenDatabaseNames()
    {
        string script = Read("docker/clean-rebuild-keyinventory-demo.sh");

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
        Assert.DoesNotContain("DROP DATABASE [KeyInventoryDev]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [DentalInventoryDemo]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE [master]", script, StringComparison.Ordinal);
        Assert.Contains("DentalInventoryDemo missing after clean rebuild", script, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanRebuildScriptIsDropletOnlyAndDemoFailClosed()
    {
        string script = Read("docker/clean-rebuild-keyinventory-demo.sh");

        Assert.Contains("DEMO / EVALUATION ONLY", script, StringComparison.Ordinal);
        Assert.Contains("Clean rebuild may run only on the Droplet under", script, StringComparison.Ordinal);
        Assert.Contains("exec \"${SELF_RUNTIME}\"", script, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_ENVIRONMENT", script, StringComparison.Ordinal);
        Assert.Contains("must be Demo when set", script, StringComparison.Ordinal);
        Assert.Contains("Database=KeyInventoryDemo", script, StringComparison.Ordinal);
        Assert.Contains("MSSQL_SA_PASSWORD is required", script, StringComparison.Ordinal);
        Assert.Contains("LocalBootstrapAdmin__Password is required", script, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanRebuildScriptPreservesSharedSqlAndForbidsVolumeDestruction()
    {
        string script = Read("docker/clean-rebuild-keyinventory-demo.sh");

        Assert.Contains("dentalinventory-demo-sql", script, StringComparison.Ordinal);
        Assert.Contains("dentalinventory-demo-web", script, StringComparison.Ordinal);
        Assert.Contains("dentalinventory-demo_default", script, StringComparison.Ordinal);
        Assert.Contains("Do not recreate SQL here", script, StringComparison.Ordinal);
        Assert.Contains("build --no-cache", script, StringComparison.Ordinal);
        Assert.Contains("run --rm migrate", script, StringComparison.Ordinal);
        Assert.Contains("up -d --force-recreate --no-deps web", script, StringComparison.Ordinal);
        Assert.Contains("/health/ready", script, StringComparison.Ordinal);
        Assert.Contains("CLEAN_REBUILD_COMPLETE", script, StringComparison.Ordinal);

        Assert.DoesNotContain("docker compose down -v", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker-compose down -v", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker volume rm", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker volume", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker system prune", script, StringComparison.Ordinal);
        Assert.DoesNotContain("rm -rf /opt/Inv", script, StringComparison.Ordinal);

        Assert.Contains("docker rm -f keyinventory-demo-web keyinventory-demo-migrate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("docker rm -f dentalinventory", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("awk '/keyinventory-demo/", script, StringComparison.Ordinal);
        Assert.Contains("docker rmi -f", script, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanRebuildScriptRestoresEnvDemoAfterClone()
    {
        string script = Read("docker/clean-rebuild-keyinventory-demo.sh");

        Assert.Contains("ENV_BACKUP=\"/tmp/keyinventory-demo.env.demo.bak\"", script, StringComparison.Ordinal);
        Assert.Contains("cp -a \"${ENV_FILE}\" \"${ENV_BACKUP}\"", script, StringComparison.Ordinal);
        Assert.Contains("cp -a \"${ENV_BACKUP}\" \"${INSTALL_DIR}/${ENV_FILE}\"", script, StringComparison.Ordinal);
        Assert.Contains("chmod 600 \"${INSTALL_DIR}/${ENV_FILE}\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadmeDocumentsCompleteCleanRebuildSeparately()
    {
        string readme = Read("docker/README-demo.md");

        Assert.Contains("## COMPLETE CLEAN REBUILD (KeyInventory only)", readme, StringComparison.Ordinal);
        Assert.Contains("docker/clean-rebuild-keyinventory-demo.sh", readme, StringComparison.Ordinal);
        Assert.Contains("./docker/clean-rebuild-keyinventory-demo.sh", readme, StringComparison.Ordinal);
        Assert.Contains("Hard-coded `/opt/keys`", readme, StringComparison.Ordinal);
        Assert.Contains("Preserves: `dentalinventory-demo-sql`", readme, StringComparison.Ordinal);
        Assert.Contains("Never runs `docker compose down -v`", readme, StringComparison.Ordinal);
        Assert.Contains("## NORMAL DEPLOY", readme, StringComparison.Ordinal);
        Assert.Contains("## RESET / RESEED DEMO", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposeAndEntrypointsDoNotInvokeCleanRebuild()
    {
        string compose = Read("docker-compose.demo.yml");
        string migrate = Read("docker/migrate-entrypoint.sh");
        string web = Read("docker/web-entrypoint.sh");

        Assert.DoesNotContain("clean-rebuild-keyinventory-demo", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("clean-rebuild-keyinventory-demo", migrate, StringComparison.Ordinal);
        Assert.DoesNotContain("clean-rebuild-keyinventory-demo", web, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP DATABASE", migrate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", web, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebContainsNoCleanRebuildEndpoint()
    {
        string webRoot = Path.Combine(RepoRoot(), "src", "KeyInventory.Web");
        List<string> hits = Directory
            .EnumerateFiles(webRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return text.Contains("clean-rebuild-keyinventory-demo", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("CLEAN_REBUILD", StringComparison.OrdinalIgnoreCase);
            })
            .Select(path => Path.GetRelativePath(RepoRoot(), path))
            .ToList();

        Assert.Empty(hits);
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

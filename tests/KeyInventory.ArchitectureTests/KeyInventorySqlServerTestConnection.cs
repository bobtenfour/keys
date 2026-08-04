using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KeyInventory.ArchitectureTests;

internal static class KeyInventorySqlServerTestConnection
{
    public static string Require()
    {
        string? connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:KeyInventory is required for persistence and workflow database tests and must target SQL Server.");
        }

        EnsureTargetsSqlServer(connectionString);
        return connectionString;
    }

    public static string RequireIsolatedDatabase()
    {
        SqlConnectionStringBuilder builder = new(Require())
        {
            InitialCatalog = $"KeyInventory_Test_{Guid.NewGuid():N}"
        };
        return builder.ConnectionString;
    }

    private static string? ResolveConnectionString()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__KeyInventory");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        string webProjectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "KeyInventory.Web"));

        if (!Directory.Exists(webProjectDirectory))
        {
            return null;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("KeyInventory");
    }

    private static void EnsureTargetsSqlServer(string connectionString)
    {
        bool looksLikeSqlite =
            connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase);

        bool looksLikeSqlServer =
            connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

        if (looksLikeSqlite || !looksLikeSqlServer)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:KeyInventory must target SQL Server. SQLite and other providers are forbidden.");
        }
    }
}

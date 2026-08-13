using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyInventoryDbContextFactory : IDesignTimeDbContextFactory<KeyInventoryDbContext>
{
    public KeyInventoryDbContext CreateDbContext(string[] args)
    {
        string connectionString = ResolveConnectionString();
        DbContextOptionsBuilder<KeyInventoryDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationCommandExecutor,
            Migrations.KeyInventoryMigrationCommandExecutor>();
        return new KeyInventoryDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string webProjectDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "KeyInventory.Web"));

        ConfigurationBuilder configurationBuilder = new();

        if (Directory.Exists(webProjectDirectory))
        {
            configurationBuilder
                .SetBasePath(webProjectDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
        }
        else if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            configurationBuilder
                .SetBasePath(currentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
        }

        configurationBuilder.AddEnvironmentVariables();

        IConfigurationRoot configuration = configurationBuilder.Build();
        string? connectionString = configuration.GetConnectionString("KeyInventory");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:KeyInventory is required for design-time DbContext creation and must target SQL Server.");
        }

        return connectionString;
    }
}

using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class DepartmentIdentityMigrationTests : IAsyncLifetime
{
    private ServiceProvider? _services;
    private string? _connectionString;

    public Task InitializeAsync()
    {
        _connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();
        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, _connectionString);
        _services = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_services is null)
        {
            return;
        }

        using (IServiceScope scope = _services.CreateScope())
        {
            KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
            await db.Database.EnsureDeletedAsync().ConfigureAwait(true);
        }

        await _services.DisposeAsync().ConfigureAwait(true);
        _services = null;
    }

    [Fact]
    public async Task EmptyDatabaseMigrateAsyncSucceedsThroughDepartmentIdentityNormalization()
    {
        using IServiceScope scope = CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await db.Database.MigrateAsync().ConfigureAwait(true);

        List<string> applied = (await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true)).ToList();
        Assert.Contains(
            applied,
            name => name.Contains("DepartmentIdentityNormalization", StringComparison.Ordinal));

        Assert.Equal(0, await db.Departments.CountAsync().ConfigureAwait(true));
        Assert.Equal(0, await db.Loans.CountAsync().ConfigureAwait(true));

        string migrationSource = await File.ReadAllTextAsync(Path.Combine(
                RepoRoot(),
                "src/KeyInventory.Infrastructure/Data/Migrations/20260812224036_DepartmentIdentityNormalization.cs"))
            .ConfigureAwait(true);
        Assert.Contains(
            "Migration stopped: WorkforceMembers row(s) have no matching Department",
            migrationSource,
            StringComparison.Ordinal);
        Assert.Contains("KeyIssuedJustificationProvenanceExtract.Apply", migrationSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ProvenanceExtractRejectsAmbiguousCodesAndMissingJustification()
    {
        Assert.True(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            "Key=OLD; WorkforceMember=WM; Justification=Department/FAC",
            out string kind,
            out string code));
        Assert.Equal("Department", kind);
        Assert.Equal("FAC", code);

        Assert.False(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            "Justification=Department/A/B",
            out _,
            out _));
        Assert.False(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            "Justification=Department/A;extra",
            out _,
            out _));
        Assert.False(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            "Subject only",
            out _,
            out _));
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
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

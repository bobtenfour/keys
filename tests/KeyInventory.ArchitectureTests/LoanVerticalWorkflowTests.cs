using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class LoanVerticalWorkflowTests : IAsyncLifetime
{
    private ServiceProvider? _services;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        _connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();

        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, _connectionString);
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(true);
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
    public async Task CreateKeyAssetSucceedsForNewKeyNumberMedecoWithClassification()
    {
        using IServiceScope scope = CreateScope();
        IListKeyAssetsUseCase list = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-100", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> keys = await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(
            keys,
            key => key.KeyNumber == "key-100" && key.MedecoKeyCode == "01" && key.Classification == KeyAccessClassification.Regular);
    }

    [Fact]
    public async Task IssueLoanSucceedsForExistingKeyAndRejectsNonUtcTimestamp()
    {
        using IServiceScope scope = CreateScope();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lv200")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-200", "01", KeyAccessClassification.Regular)
            .ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset due = issued.AddDays(1);
        await issue.ExecuteAsync(
                "loan-200",
                "key-200",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                due,
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> openLoans = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(openLoans, loan => loan.LoanCode == "loan-200" && loan.BorrowerPartyReference == seeded.PartyCode);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-201", "01", KeyAccessClassification.Regular)
            .ConfigureAwait(true);
        DateTimeOffset nonUtc = new(2026, 8, 3, 12, 0, 0, TimeSpan.FromHours(-5));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            issue.ExecuteAsync(
                "loan-201",
                "key-201",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                nonUtc,
                nonUtc.AddDays(1),
                CancellationToken.None));
    }

    [Fact]
    public async Task CompleteReturnSucceedsForOpenLoanAndRejectsNonOpenLoan()
    {
        using IServiceScope scope = CreateScope();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListReturnedLoansUseCase listReturned = scope.ServiceProvider.GetRequiredService<IListReturnedLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lv300")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-300", "01", KeyAccessClassification.Regular)
            .ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-300",
                "key-300",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await completeReturn.ExecuteAsync("return-300", "loan-300", issued.AddHours(2), CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> returned = await listReturned.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(
            returned,
            loan => loan.LoanCode == "loan-300"
                && loan.KeyNumber == "key-300"
                && loan.MedecoKeyCode == "01");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            completeReturn.ExecuteAsync("return-301", "loan-300", issued.AddHours(3), CancellationToken.None));
    }

    [Fact]
    public async Task ListOpenAndReturnedLoansReturnExpectedResults()
    {
        using IServiceScope scope = CreateScope();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();
        IListReturnedLoansUseCase listReturned = scope.ServiceProvider.GetRequiredService<IListReturnedLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lv400")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-400", "01", KeyAccessClassification.Regular)
            .ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-open",
                "key-400",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "key-401", "01", KeyAccessClassification.Regular)
            .ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-done",
                "key-401",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);
        await completeReturn.ExecuteAsync("return-done", "loan-done", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> openLoans = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<LoanListItem> returnedLoans = await listReturned.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Contains(openLoans, loan => loan.LoanCode == "loan-open");
        Assert.DoesNotContain(openLoans, loan => loan.LoanCode == "loan-done");
        Assert.Contains(returnedLoans, loan => loan.LoanCode == "loan-done");
        Assert.DoesNotContain(returnedLoans, loan => loan.LoanCode == "loan-open");
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}

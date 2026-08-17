using System.Reflection;
using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Web.Pages.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyPhysicalLifecycleWorkflowTests : IAsyncLifetime
{
    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        string connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();
        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, connectionString);
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

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }

    [Fact]
    public void DomainConditionAndLoanClosureAuthoritiesExistWithoutRetireActivate()
    {
        KeyAsset key = CatalogTestFactory.CreateCopy("LIFE-D", "01");
        Assert.Equal(KeyPhysicalCondition.Active, key.Condition);
        Assert.True(key.IsIssuableCondition);

        key.MarkLost();
        Assert.Equal(KeyPhysicalCondition.Lost, key.Condition);
        Assert.False(key.IsIssuableCondition);
        Assert.Throws<InvalidOperationException>(() => key.MarkLost());

        key.Destroy();
        Assert.Equal(KeyPhysicalCondition.Destroyed, key.Condition);
        Assert.Throws<InvalidOperationException>(() => key.Destroy());

        Assert.Null(typeof(KeyAsset).GetMethod("Activate"));
        Assert.Null(typeof(KeyAsset).GetMethod("Retire"));
        Assert.Null(typeof(KeyAsset).GetProperty("IsActive"));

        KeyAsset issued = CatalogTestFactory.CreateCopy("LIFE-L", "01");
        DateTimeOffset issuedAt = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        Loan loan = new(
            "loan-life-close",
            issued,
            "party-1",
            issuedAt,
            issuedAt.AddDays(1),
            KeyIssueJustificationKind.Department,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "DEPT",
            null);
        loan.CloseAsLost();
        Assert.Equal(LoanStatus.Lost, loan.Status);
        Assert.False(loan.IsOpenForReturn);
        Assert.Throws<InvalidOperationException>(() =>
            new Return("ret-bad", loan, issuedAt.AddHours(1)));
    }

    [Fact]
    public async Task AvailableIssuedLostDestroyReplaceAndReceiveBehaviors()
    {
        using IServiceScope scope = CreateScope();
        IListKeyAssetsUseCase list = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        ISearchAvailableKeyCopiesUseCase searchAvailable =
            scope.ServiceProvider.GetRequiredService<ISearchAvailableKeyCopiesUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IMarkKeyAssetLostUseCase markLost = scope.ServiceProvider.GetRequiredService<IMarkKeyAssetLostUseCase>();
        IDestroyKeyAssetUseCase destroy = scope.ServiceProvider.GetRequiredService<IDestroyKeyAssetUseCase>();
        IReplaceLostKeyUseCase replace = scope.ServiceProvider.GetRequiredService<IReplaceLostKeyUseCase>();
        ISearchLostKeysUseCase searchLost = scope.ServiceProvider.GetRequiredService<ISearchLostKeysUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        ILoanPersistencePort loans = scope.ServiceProvider.GetRequiredService<ILoanPersistencePort>();
        IOperatorAuditTrailUseCase audit = scope.ServiceProvider.GetRequiredService<IOperatorAuditTrailUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "life")
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "LIFE-1", "01", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "LIFE-1", "02", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> keys = await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        KeyAssetListItem available = keys.Single(item => item.MedecoKeyCode == "01");
        KeyAssetListItem toIssue = keys.Single(item => item.MedecoKeyCode == "02");
        Assert.Equal(KeyPhysicalCondition.Active, available.Condition);

        IReadOnlyList<AvailableKeyCopyCandidate> availableCandidates = await searchAvailable
            .ExecuteAsync("LIFE-1", ISearchAvailableKeyCopiesUseCase.DefaultMaxResults, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(availableCandidates, item => item.MedecoKeyCode == "01");
        Assert.Contains(availableCandidates, item => item.MedecoKeyCode == "02");

        DateTimeOffset issuedAt = new(2026, 8, 14, 15, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-life-1",
                "LIFE-1",
                "02",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issuedAt,
                issuedAt.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                issue.ExecuteAsync(
                    "loan-life-dup",
                    "LIFE-1",
                    "02",
                    seeded.MemberCode,
                    "Department",
                    seeded.DepartmentCode,
                    issuedAt.AddHours(1),
                    issuedAt.AddDays(2),
                    CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> afterIssue = await lookup.SearchKeysAsync("LIFE-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(
            afterIssue,
            item => item.MedecoKeyCode == "02"
                && item.Condition == KeyPhysicalCondition.Active
                && item.AvailabilityStatus == OperationalKeyAvailability.Issued);
        Assert.Contains(
            afterIssue,
            item => item.MedecoKeyCode == "01"
                && item.Condition == KeyPhysicalCondition.Active
                && item.AvailabilityStatus == OperationalKeyAvailability.Available);

        await markLost.ExecuteAsync(toIssue.KeyAssetId, CancellationToken.None).ConfigureAwait(true);
        LoanListItem? closedLost = (await loans.ListClosedLoansAsync(CancellationToken.None).ConfigureAwait(true))
            .SingleOrDefault(item => item.LoanCode == "loan-life-1");
        Assert.NotNull(closedLost);
        Assert.Equal(nameof(LoanStatus.Lost), closedLost.Status);

        KeyAssetListItem lostKey = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyAssetId == toIssue.KeyAssetId);
        Assert.Equal(KeyPhysicalCondition.Lost, lostKey.Condition);

        IReadOnlyList<KeyLookupResult> afterLost = await lookup.SearchKeysAsync("LIFE-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(
            afterLost,
            item => item.MedecoKeyCode == "02"
                && item.Condition == KeyPhysicalCondition.Lost
                && string.IsNullOrEmpty(item.AvailabilityStatus));
        Assert.DoesNotContain(
            afterLost,
            item => item.MedecoKeyCode == "02"
                && item.AvailabilityStatus == OperationalKeyAvailability.Available);

        IReadOnlyList<AvailableKeyCopyCandidate> afterLostAvailable = await searchAvailable
            .ExecuteAsync("LIFE-1", ISearchAvailableKeyCopiesUseCase.DefaultMaxResults, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.DoesNotContain(afterLostAvailable, item => item.MedecoKeyCode == "02");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replace.ExecuteAsync(available.KeyAssetId, "99", CancellationToken.None))
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                issue.ExecuteAsync(
                    "loan-life-lost",
                    "LIFE-1",
                    "02",
                    seeded.MemberCode,
                    "Department",
                    seeded.DepartmentCode,
                    issuedAt.AddHours(2),
                    issuedAt.AddDays(3),
                    CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<LostKeyCandidate> lostCandidates = await searchLost
            .ExecuteAsync("LIFE-1", ISearchLostKeysUseCase.DefaultMaxResults, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(lostCandidates, item => item.KeyAssetId == toIssue.KeyAssetId);

        Guid replacementId = await replace.ExecuteAsync(toIssue.KeyAssetId, "03", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotEqual(toIssue.KeyAssetId, replacementId);

        KeyAssetListItem source = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyAssetId == toIssue.KeyAssetId);
        KeyAssetListItem replacement = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyAssetId == replacementId);
        Assert.Equal(KeyPhysicalCondition.Lost, source.Condition);
        Assert.Equal(KeyPhysicalCondition.Active, replacement.Condition);
        Assert.Equal("LIFE-1", replacement.KeyNumber);
        Assert.Equal("03", replacement.MedecoKeyCode);
        Assert.Equal(toIssue.KeyAssetId, replacement.ReplacesKeyAssetId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replace.ExecuteAsync(toIssue.KeyAssetId, "03", CancellationToken.None))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replace.ExecuteAsync(replacementId, "04", CancellationToken.None))
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "LIFE-1", "04", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        KeyAssetListItem receiveKey = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.MedecoKeyCode == "04");
        await issue.ExecuteAsync(
                "loan-life-ret",
                "LIFE-1",
                "04",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issuedAt.AddHours(3),
                issuedAt.AddDays(4),
                CancellationToken.None)
            .ConfigureAwait(true);
        await completeReturn.ExecuteAsync(
                "return-life-ret",
                "loan-life-ret",
                issuedAt.AddHours(4),
                CancellationToken.None)
            .ConfigureAwait(true);
        LoanListItem returned = (await loans.ListClosedLoansAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.LoanCode == "loan-life-ret");
        Assert.Equal(nameof(LoanStatus.Returned), returned.Status);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "LIFE-1", "05", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        KeyAssetListItem destroyIssued = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.MedecoKeyCode == "05");
        await issue.ExecuteAsync(
                "loan-life-des",
                "LIFE-1",
                "05",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issuedAt.AddHours(5),
                issuedAt.AddDays(5),
                CancellationToken.None)
            .ConfigureAwait(true);
        await destroy.ExecuteAsync(destroyIssued.KeyAssetId, CancellationToken.None).ConfigureAwait(true);
        LoanListItem closedDestroyed = (await loans.ListClosedLoansAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.LoanCode == "loan-life-des");
        Assert.Equal(nameof(LoanStatus.Destroyed), closedDestroyed.Status);
        Assert.Equal(
            KeyPhysicalCondition.Destroyed,
            (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
                .Single(item => item.KeyAssetId == destroyIssued.KeyAssetId)
                .Condition);

        await destroy.ExecuteAsync(toIssue.KeyAssetId, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            KeyPhysicalCondition.Destroyed,
            (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
                .Single(item => item.KeyAssetId == toIssue.KeyAssetId)
                .Condition);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replace.ExecuteAsync(toIssue.KeyAssetId, "06", CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<KeyAssetLifecycleItem> catalog = await lifecycle.ListKeyAssetsAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.All(catalog, item =>
        {
            Assert.False(item.Capabilities.CanRetire);
            Assert.False(item.Capabilities.CanActivate);
        });

        IReadOnlyList<OperatorAuditTrailItem> audits = await audit
            .QueryAsync(new OperatorAuditTrailQuery(null, null, null, null, null), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.KeyMarkedLost);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.CustodyClosedLost);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.LostKeyReplaced);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.KeyDestroyed);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.CustodyClosedDestroyed);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.KeyReturned);
    }

    [Fact]
    public void RegisterExposesExactlyTwoModesAndOperatorVocabulary()
    {
        Assert.Equal("New", RegisterModel.ModeNew);
        Assert.Equal("Replace", RegisterModel.ModeReplace);

        string registerPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "KeyInventory.Web",
            "Pages",
            "Catalog",
            "Register.cshtml");
        string markup = File.ReadAllText(registerPath);
        Assert.Contains("New Key", markup, StringComparison.Ordinal);
        Assert.Contains("Replace Lost Key", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Add New Key", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Create New KEY #", markup, StringComparison.Ordinal);
        string codeBehind = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "KeyInventory.Web",
            "Pages",
            "Catalog",
            "Register.cshtml.cs"));
        Assert.Contains("\"Create Key\"", codeBehind, StringComparison.Ordinal);
        Assert.Contains("\"Replace Key\"", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Add Key\"", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Register physical copy", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("physical MEDECO copy", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MEDECO Key Code", markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asp-route-mode", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-page-handler=\"SetMode\"", markup, StringComparison.Ordinal);

        Type lifecycle = typeof(IConfigurationLifecycleUseCase);
        Assert.Null(lifecycle.GetMethod("ActivateKeyAssetAsync"));
        Assert.Null(lifecycle.GetMethod("RetireKeyAssetAsync"));

        using IServiceScope scope = CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IMarkKeyAssetLostUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IDestroyKeyAssetUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IReplaceLostKeyUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<ISearchLostKeysUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICreateKeyAssetUseCase>());
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Could not locate KeyInventory.sln from test base directory.");
    }
}

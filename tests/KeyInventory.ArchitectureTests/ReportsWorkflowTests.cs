using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class ReportsWorkflowTests : IAsyncLifetime
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

    [Fact]
    public async Task AuthorizedReportsReadSqlBackedDataWithHoldersOverdueMemberHistoryAndCatalog()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        ITerminateWorkforceMemberUseCase terminate = scope.ServiceProvider.GetRequiredService<ITerminateWorkforceMemberUseCase>();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "rp")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "RP-KEY-1", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "RP-KEY-2", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "RP-KEY-3", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset duePast = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset dueFuture = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

        await issue.ExecuteAsync(
                "loan-rp-1",
                "RP-KEY-1",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                duePast,
                CancellationToken.None)
            .ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-rp-2",
                "RP-KEY-2",
                "01",
                seeded.MemberCode,
                "Room",
                seeded.RoomCode,
                issued,
                dueFuture,
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<CurrentKeyHolderReportRow> holders =
            await reports.ListCurrentKeyHoldersAsync("RP-KEY", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(2, holders.Count);
        Assert.Contains(holders, row =>
            row.KeyNumber == "RP-KEY-1" && row.MedecoKeyCode == "01"
            && row.HolderFirstName == "Ada"
            && row.HolderLastName == "Lovelace"
            && row.WorkforceMemberCode == seeded.MemberCode
            && row.DepartmentCode == seeded.DepartmentCode);

        IReadOnlyList<ActiveLoanReportRow> active =
            await reports.ListActiveLoansReportAsync(null, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(2, active.Count);

        IReadOnlyList<OverdueKeyReportRow> overdue =
            await reports.ListOverdueKeysAsync(now, null, CancellationToken.None).ConfigureAwait(true);
        Assert.Single(overdue);
        Assert.Equal("RP-KEY-1", overdue[0].KeyNumber);
        Assert.Equal("01", overdue[0].MedecoKeyCode);
        Assert.Equal(6, overdue[0].DaysOverdue);
        Assert.Equal(seeded.MemberCode, overdue[0].WorkforceMemberCode);

        await completeReturn.ExecuteAsync(
                "return-rp-2",
                "loan-rp-2",
                issued.AddHours(3),
                CancellationToken.None)
            .ConfigureAwait(true);

        KeysByWorkforceMemberReport? memberReport = await reports
            .GetKeysByWorkforceMemberAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(memberReport);
        Assert.Single(memberReport!.IssuedKeys);
        Assert.Equal("RP-KEY-1", memberReport.IssuedKeys[0].KeyNumber);
        Assert.Equal("01", memberReport.IssuedKeys[0].MedecoKeyCode);
        Assert.Single(memberReport.ReturnedKeys);
        Assert.Equal("RP-KEY-2", memberReport.ReturnedKeys[0].KeyNumber);
        Assert.Equal("01", memberReport.ReturnedKeys[0].MedecoKeyCode);

        IReadOnlyList<KeyHistoryReportRow> history =
            await reports.ListKeyHistoryAsync("RP-KEY-2", CancellationToken.None).ConfigureAwait(true);
        Assert.Single(history);
        Assert.Equal(nameof(KeyInventory.Domain.Loans.LoanStatus.Returned), history[0].Status);
        Assert.NotNull(history[0].ReturnedAtUtc);

        await terminate.ExecuteAsync(seeded.MemberCode, CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<OutstandingWorkforceKeyReportRow> outstanding = await reports
            .ListOutstandingKeysByWorkforceStatusAsync("Terminated", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(outstanding, row =>
            row.WorkforceMemberCode == seeded.MemberCode
            && row.WorkforceMemberStatus == "Terminated"
            && row.KeyNumber == "RP-KEY-1" && row.MedecoKeyCode == "01");

        IReadOnlyList<KeyCatalogReportRow> catalog =
            await reports.ListKeyCatalogReportAsync("RP-KEY", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(3, catalog.Count);
        Assert.Contains(catalog, row =>
            row.KeyNumber == "RP-KEY-1" && row.MedecoKeyCode == "01" && row.AvailabilityStatus == OperationalKeyAvailability.Issued);
        Assert.Contains(catalog, row =>
            row.KeyNumber == "RP-KEY-3" && row.MedecoKeyCode == "01" && row.AvailabilityStatus == OperationalKeyAvailability.Available);

        string csv = reports.FormatCurrentKeyHoldersCsv(
            await reports.ListCurrentKeyHoldersAsync(null, CancellationToken.None).ConfigureAwait(true));
        Assert.Contains("Holder First Name", csv, StringComparison.Ordinal);
        Assert.Contains("Ada", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Justification", csv, StringComparison.OrdinalIgnoreCase);

        string escaped = ReportCsvFormatter.Escape("Ada, \"Lovelace\"");
        Assert.Equal("\"Ada, \"\"Lovelace\"\"\"", escaped);

        string memberCsv = reports.FormatKeysByWorkforceMemberCsv(memberReport);
        Assert.Contains("RP-KEY-1", memberCsv, StringComparison.Ordinal);
        Assert.Contains("RP-KEY-2", memberCsv, StringComparison.Ordinal);
        Assert.Equal(memberCsv, reports.FormatKeysByWorkforceMemberCsv(memberReport));
    }

    [Fact]
    public async Task ExistingIssueAndReceiveRemainValidWithReportsRegistered()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "rp-flow")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "rp-flow-key", "01", KeyAccessClassification.Regular).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 8, 15, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-rp-flow",
                "rp-flow-key",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Contains(
            await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true),
            loan => loan.LoanCode == "loan-rp-flow");

        await completeReturn.ExecuteAsync("return-rp-flow", "loan-rp-flow", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.DoesNotContain(
            await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true),
            loan => loan.LoanCode == "loan-rp-flow");
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}

using KeyInventory.Application.Catalog;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Readiness;
using KeyInventory.Application.Workflow;
using KeyInventory.Application.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.OperatorAudit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorExperienceFirstUseWorkflowTests : IAsyncLifetime
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
        OperatorIdentityAccessor.TestOperatorReference.Value = null;
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
    public async Task EmptyDatabaseFirstUseReachesIssueAndReceiveWithoutOrganizationBuildingOrManager()
    {
        using IServiceScope scope = CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        OperatorIdentityAccessor.TestOperatorReference.Value = "first-use-operator";

        OperationalReadinessSnapshot empty = await services.GetRequiredService<IOperationalReadinessUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.False(empty.HasDepartment);
        Assert.False(empty.HasRoom);
        Assert.False(empty.HasKey);
        Assert.False(empty.HasWorkforceMember);
        Assert.False(empty.CanIssueKey);

        await services.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync("FACILITIES", CancellationToken.None)
            .ConfigureAwait(true);
        string roomCode = await services.GetRequiredService<ICreateRoomUseCase>()
            .ExecuteAsync("FACILITIES", "101", "Key room", CancellationToken.None)
            .ConfigureAwait(true);

        string memberCode = await services.GetRequiredService<IRegisterWorkforceMemberUseCase>()
            .ExecuteAsync("Ada", "Lovelace", "123456789", "Employee", "FACILITIES", CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Single(await services.GetRequiredService<IListWorkforceMembersUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true));

        await services.GetRequiredService<ICreateWorkAssignmentUseCase>()
            .ExecuteAsync(memberCode, roomCode, CancellationToken.None)
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(services, "KEY-101", "01", KeyAccessClassification.Regular, roomCode)
            .ConfigureAwait(true);

        OperationalReadinessSnapshot ready = await services.GetRequiredService<IOperationalReadinessUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.True(ready.CanIssueKey);
        Assert.True(ready.HasValidKeyAccess);

        DateTimeOffset issued = DateTimeOffset.UtcNow;
        DateTimeOffset due = issued.AddHours(8);
        await services.GetRequiredService<IIssueLoanUseCase>()
            .ExecuteAsync(
                "LOAN-1",
                "KEY-101",
                "01",
                memberCode,
                "Department",
                "FACILITIES",
                issued,
                due,
                CancellationToken.None)
            .ConfigureAwait(true);

        await services.GetRequiredService<ICompleteReturnUseCase>()
            .ExecuteAsync("RET-1", "LOAN-1", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);

        string partyCode = (await services.GetRequiredService<IListWorkforceMembersUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true))[0].PartyCode;

        await services.GetRequiredService<ICorrectPartyUinUseCase>()
            .ExecuteAsync(partyCode, "987654321", CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<OperatorAuditTrailItem> audits = await services.GetRequiredService<IOperatorAuditTrailUseCase>()
            .QueryAsync(new OperatorAuditTrailQuery(null, null, null, null, null), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(
            audits,
            item => item.ActionType == OperatorAuditActions.PartyUinCorrected
                && item.OperatorReference == "first-use-operator"
                && item.Details.Contains("123456789", StringComparison.Ordinal)
                && item.Details.Contains("987654321", StringComparison.Ordinal));

        KeyInventoryDbContext db = services.GetRequiredService<KeyInventoryDbContext>();
        string[] tableNames = db.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
        Assert.DoesNotContain("Organizations", tableNames);
        Assert.DoesNotContain("Buildings", tableNames);
        Assert.DoesNotContain(
            typeof(KeyInventory.Domain.Workforce.WorkforceMember).GetProperties().Select(property => property.Name),
            name => name.Contains("Organization", StringComparison.Ordinal)
                || name.Contains("ResponsibleManager", StringComparison.Ordinal));
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}

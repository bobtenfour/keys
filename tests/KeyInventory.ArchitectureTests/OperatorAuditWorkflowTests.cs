using KeyInventory.Application.Catalog;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.OperatorAudit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorAuditWorkflowTests : IAsyncLifetime
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
    public async Task AuthenticatedOperatorIsCapturedAndDistinctFromWorkforceMemberSubject()
    {
        using IServiceScope scope = CreateScope();
        OperatorIdentityAccessor.TestOperatorReference.Value = "audit-operator";
        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "aud1")
            .ConfigureAwait(true);

        IReadOnlyList<OperatorAuditTrailItem> rows = await QueryAsync(scope, subject: seeded.MemberCode)
            .ConfigureAwait(true);
        Assert.Contains(
            rows,
            item => item.ActionType == OperatorAuditActions.WorkforceMemberCreated
                && item.OperatorReference == "audit-operator"
                && item.SubjectReference == seeded.MemberCode
                && !string.Equals(item.OperatorReference, item.SubjectReference, StringComparison.Ordinal));
        Assert.All(rows, item => Assert.Equal(TimeSpan.Zero, item.OccurredAtUtc.Offset));
    }

    [Fact]
    public async Task KeyRegisterRoomIssueReturnAndAdminMutationsAreAudited()
    {
        using IServiceScope scope = CreateScope();
        OperatorIdentityAccessor.TestOperatorReference.Value = "ops-user";
        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "aud2")
            .ConfigureAwait(true);

        await scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>()
            .ExecuteAsync("AUD-KEY-1", "aud-type", CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IKeyRoomAssignmentUseCase>()
            .AssignRoomAsync("AUD-KEY-1", seeded.RoomCode, CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IKeyRoomAssignmentUseCase>()
            .RemoveRoomAsync("AUD-KEY-1", seeded.RoomCode, CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IKeyRoomAssignmentUseCase>()
            .AssignRoomAsync("AUD-KEY-1", seeded.RoomCode, CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset issued = DateTimeOffset.UtcNow;
        DateTimeOffset due = issued.AddDays(1);
        await scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>()
            .ExecuteAsync(
                "loan-aud-1",
                "AUD-KEY-1",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                due,
                CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>()
            .ExecuteAsync("ret-aud-1", "loan-aud-1", DateTimeOffset.UtcNow, CancellationToken.None)
            .ConfigureAwait(true);

        await scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberWorkforceTypeUseCase>()
            .ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IEndWorkAssignmentUseCase>()
            .ExecuteAsync("aud2-wa-1", CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IUpdateRoomNumberUseCase>()
            .ExecuteAsync(seeded.RoomCode, "999", CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IRetireRoomUseCase>()
            .ExecuteAsync(seeded.RoomCode, CancellationToken.None)
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<IActivateRoomUseCase>()
            .ExecuteAsync(seeded.RoomCode, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<OperatorAuditTrailItem> all = await QueryAsync(scope).ConfigureAwait(true);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyTypeCreated && item.OperatorReference == "ops-user");
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyRegistered);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyRoomAssignmentAdded);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyRoomAssignmentRemoved);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyIssued);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.KeyReturned);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.WorkforceMemberMaintained);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.WorkAssignmentEnded);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.RoomUpdated);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.RoomRetired);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.RoomActivated);
        Assert.Contains(all, item => item.ActionType == OperatorAuditActions.WorkAssignmentCreated);

        IReadOnlyList<OperatorAuditTrailItem> filtered = await QueryAsync(
                scope,
                operatorReference: "ops-user",
                actionType: OperatorAuditActions.KeyIssued)
            .ConfigureAwait(true);
        Assert.NotEmpty(filtered);
        Assert.All(filtered, item => Assert.Equal(OperatorAuditActions.KeyIssued, item.ActionType));
    }

    [Fact]
    public async Task FailedMutationDoesNotPersistAuditAndPersistencePortHasNoDelete()
    {
        using IServiceScope scope = CreateScope();
        OperatorIdentityAccessor.TestOperatorReference.Value = "fail-user";
        await scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync("aud-fail-dept", CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
                    .ExecuteAsync("aud-fail-dept", CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<OperatorAuditTrailItem> created = await QueryAsync(
                scope,
                actionType: OperatorAuditActions.DepartmentCreated,
                subject: "aud-fail-dept")
            .ConfigureAwait(true);
        Assert.Single(created);

        Type port = typeof(IOperatorAuditPersistencePort);
        Assert.DoesNotContain(
            port.GetMethods(),
            method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RequiredAuditFailurePreventsSuccessfulMutationWhenOperatorMissing()
    {
        using IServiceScope scope = CreateScope();
        OperatorIdentityAccessor.TestOperatorReference.Value = OperatorIdentityAccessor.DenyOperatorMarker;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
                    .ExecuteAsync("aud-no-op-dept", CancellationToken.None))
            .ConfigureAwait(true);

        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        Assert.False(await db.Departments.AnyAsync(entity => entity.DepartmentCode == "aud-no-op-dept").ConfigureAwait(true));
        Assert.False(await db.OperatorAuditRecords.AnyAsync(entity => entity.SubjectReference == "aud-no-op-dept")
            .ConfigureAwait(true));
    }

    [Fact]
    public async Task TerminateIsAudited()
    {
        using IServiceScope scope = CreateScope();
        OperatorIdentityAccessor.TestOperatorReference.Value = "term-user";
        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "aud3")
            .ConfigureAwait(true);

        await scope.ServiceProvider.GetRequiredService<ITerminateWorkforceMemberUseCase>()
            .ExecuteAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<OperatorAuditTrailItem> rows = await QueryAsync(
                scope,
                actionType: OperatorAuditActions.WorkforceMemberTerminated,
                subject: seeded.MemberCode)
            .ConfigureAwait(true);
        Assert.Single(rows);
        Assert.Equal("term-user", rows[0].OperatorReference);
    }

    private static async Task<IReadOnlyList<OperatorAuditTrailItem>> QueryAsync(
        IServiceScope scope,
        string? operatorReference = null,
        string? actionType = null,
        string? subject = null)
    {
        return await scope.ServiceProvider.GetRequiredService<IOperatorAuditTrailUseCase>()
            .QueryAsync(
                new OperatorAuditTrailQuery(null, null, operatorReference, actionType, subject),
                CancellationToken.None)
            .ConfigureAwait(true);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}

using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class DepartmentIdentityNormalizationTests : IAsyncLifetime
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
    public async Task DepartmentIdStableAndCodeEditable()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IUpdateDepartmentCodeUseCase updateCode =
            scope.ServiceProvider.GetRequiredService<IUpdateDepartmentCodeUseCase>();
        IRegisterWorkforceMemberUseCase register =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        IListDepartmentsUseCase listDepts = scope.ServiceProvider.GetRequiredService<IListDepartmentsUseCase>();
        IListWorkforceMembersUseCase listMembers =
            scope.ServiceProvider.GetRequiredService<IListWorkforceMembersUseCase>();
        IWorkforcePersistencePort workforce = scope.ServiceProvider.GetRequiredService<IWorkforcePersistencePort>();

        await createDept.ExecuteAsync("norm-dept", CancellationToken.None).ConfigureAwait(true);
        DepartmentListItem before = (await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "norm-dept");
        string memberCode = await register.ExecuteAsync(
                "Ada",
                "Lovelace",
                "111222333",
                nameof(WorkforceType.Employee),
                "norm-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        await updateCode.ExecuteAsync(before.DepartmentId, "norm-dept-renamed", CancellationToken.None)
            .ConfigureAwait(true);

        DepartmentListItem after = (await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentId == before.DepartmentId);
        Assert.Equal(before.DepartmentId, after.DepartmentId);
        Assert.Equal("norm-dept-renamed", after.DepartmentCode);
        Assert.Equal(0, (await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Count(item => item.DepartmentCode == "norm-dept"));

        WorkforceMemberListItem member = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == memberCode);
        Assert.Equal("norm-dept-renamed", member.DepartmentCode);

        WorkforceMember? domainMember = await workforce
            .FindWorkforceMemberAsync(memberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(domainMember);
        Assert.Equal(before.DepartmentId, domainMember.DepartmentId);
    }

    [Fact]
    public async Task UnusedDepartmentEditDelete()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("unused-edit", CancellationToken.None).ConfigureAwait(true);
        DepartmentLifecycleItem unused = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "unused-edit");
        Assert.True(unused.Capabilities.CanEdit);
        Assert.True(unused.Capabilities.CanDelete);
        Assert.False(unused.Capabilities.CanRetire);

        await lifecycle.DeleteDepartmentAsync(unused.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            0,
            await db.Departments.CountAsync(item => item.DepartmentId == unused.DepartmentId).ConfigureAwait(true));
    }

    [Fact]
    public async Task ReferencedDepartmentRetireNotDelete()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IUpdateDepartmentCodeUseCase updateCode =
            scope.ServiceProvider.GetRequiredService<IUpdateDepartmentCodeUseCase>();
        IRegisterWorkforceMemberUseCase register =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();

        await createDept.ExecuteAsync("ref-dept", CancellationToken.None).ConfigureAwait(true);
        await register.ExecuteAsync(
                "Grace",
                "Hopper",
                "444555666",
                nameof(WorkforceType.Employee),
                "ref-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        DepartmentLifecycleItem referenced = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "ref-dept");
        Assert.True(referenced.Capabilities.CanEdit);
        Assert.False(referenced.Capabilities.CanDelete);
        Assert.True(referenced.Capabilities.CanRetire);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                lifecycle.DeleteDepartmentAsync(referenced.DepartmentId, CancellationToken.None))
            .ConfigureAwait(true);

        await updateCode.ExecuteAsync(referenced.DepartmentId, "ref-dept-renamed", CancellationToken.None)
            .ConfigureAwait(true);
        DepartmentLifecycleItem afterRename = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentId == referenced.DepartmentId);
        Assert.False(afterRename.Capabilities.CanDelete);

        await lifecycle.RetireDepartmentAsync(referenced.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        DepartmentLifecycleItem retired = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentId == referenced.DepartmentId);
        Assert.False(retired.IsActive);
        Assert.True(retired.Capabilities.CanActivate);

        await lifecycle.ActivateDepartmentAsync(referenced.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        Assert.True((await lifecycle.ListDepartmentsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentId == referenced.DepartmentId).IsActive);
    }

    [Fact]
    public async Task IssueJustificationSnapshotSurvivesRename()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IUpdateDepartmentCodeUseCase updateCode =
            scope.ServiceProvider.GetRequiredService<IUpdateDepartmentCodeUseCase>();
        IOperatorAuditTrailUseCase auditTrail =
            scope.ServiceProvider.GetRequiredService<IOperatorAuditTrailUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "snap")
            .ConfigureAwait(true);
        Guid departmentId = (await scope.ServiceProvider.GetRequiredService<IListDepartmentsUseCase>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == seeded.DepartmentCode).DepartmentId;

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "SNAP-KEY", "01", KeyAccessClassification.Regular).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 12, 16, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-snap-1",
                "SNAP-KEY",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        LoanEntity loanBefore = await db.Loans.AsNoTracking()
            .SingleAsync(item => item.LoanCode == "loan-snap-1")
            .ConfigureAwait(true);
        Assert.Equal(nameof(KeyIssueJustificationKind.Department), loanBefore.JustificationKind);
        Assert.Equal(departmentId, loanBefore.JustificationDepartmentId);
        Assert.Equal(seeded.DepartmentCode, loanBefore.JustificationDepartmentCodeSnapshot);

        OperatorAuditTrailItem issuedAudit = (await auditTrail
                .QueryAsync(
                    new OperatorAuditTrailQuery(null, null, null, OperatorAuditActions.KeyIssued, "loan-snap-1"),
                    CancellationToken.None)
                .ConfigureAwait(true))
            .Single();
        string detailsBefore = issuedAudit.Details;

        await updateCode.ExecuteAsync(departmentId, "snap-dept-renamed", CancellationToken.None)
            .ConfigureAwait(true);

        LoanEntity loanAfter = await db.Loans.AsNoTracking()
            .SingleAsync(item => item.LoanCode == "loan-snap-1")
            .ConfigureAwait(true);
        Assert.Equal(departmentId, loanAfter.JustificationDepartmentId);
        Assert.Equal(seeded.DepartmentCode, loanAfter.JustificationDepartmentCodeSnapshot);
        Assert.NotEqual("snap-dept-renamed", loanAfter.JustificationDepartmentCodeSnapshot);

        OperatorAuditTrailItem issuedAuditAfter = (await auditTrail
                .QueryAsync(
                    new OperatorAuditTrailQuery(null, null, null, OperatorAuditActions.KeyIssued, "loan-snap-1"),
                    CancellationToken.None)
                .ConfigureAwait(true))
            .Single();
        Assert.Equal(detailsBefore, issuedAuditAfter.Details);
        Assert.Contains($"Justification=Department/{seeded.DepartmentCode}", detailsBefore, StringComparison.Ordinal);
    }

    [Fact]
    public void LoanJustificationInvariants()
    {
        KeyAsset keyAsset = CatalogTestFactory.CreateCopy("inv-key", "01", KeyAccessClassification.Regular);
        DateTimeOffset issued = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset due = issued.AddDays(1);
        Guid departmentId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        Assert.Throws<ArgumentException>(() => new Loan(
            "loan-inv-1",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Department,
            departmentId,
            "DEPT",
            "ROOM-1"));

        Assert.Throws<ArgumentException>(() => new Loan(
            "loan-inv-2",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Department,
            departmentId,
            null,
            null));

        Assert.Throws<ArgumentException>(() => new Loan(
            "loan-inv-3",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Department,
            departmentId,
            " ",
            null));

        Assert.Throws<ArgumentException>(() => new Loan(
            "loan-inv-4",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Room,
            departmentId,
            null,
            "ROOM-1"));

        Loan departmentLoan = new(
            "loan-inv-ok-dept",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Department,
            departmentId,
            "DEPT",
            null);
        Assert.Equal(KeyIssueJustificationKind.Department, departmentLoan.JustificationKind);
        Assert.Equal(departmentId, departmentLoan.JustificationDepartmentId);
        Assert.Equal("DEPT", departmentLoan.JustificationDepartmentCodeSnapshot);
        Assert.Null(departmentLoan.JustificationRoomCode);

        Loan roomLoan = new(
            "loan-inv-ok-room",
            keyAsset,
            "party-1",
            issued,
            due,
            KeyIssueJustificationKind.Room,
            null,
            null,
            "ROOM-1");
        Assert.Equal(KeyIssueJustificationKind.Room, roomLoan.JustificationKind);
        Assert.Equal("ROOM-1", roomLoan.JustificationRoomCode);
        Assert.Null(roomLoan.JustificationDepartmentId);
    }

    [Fact]
    public void ConfigurationLifecycleDoesNotParseAuditDetails()
    {
        string source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Application/Lifecycle/ConfigurationLifecycleUseCase.cs"));
        Assert.DoesNotContain("IOperatorAuditPersistencePort", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Justification=Department", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Justification=Room", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Details.Contains", source, StringComparison.Ordinal);
        Assert.Contains("CountLoansJustifiedByDepartment", source, StringComparison.Ordinal);
        Assert.Contains("CountLoansJustifiedByRoom", source, StringComparison.Ordinal);

        const string evaluateSignature =
            "private async Task<(bool CanDelete, string? BlockedReason)> EvaluateDepartmentDeleteAsync";
        int evaluateIndex = source.IndexOf(evaluateSignature, StringComparison.Ordinal);
        Assert.True(evaluateIndex >= 0);
        int roomEvaluateIndex = source.IndexOf(
            "private async Task<(bool CanDelete, string? BlockedReason)> EvaluateRoomDeleteAsync",
            evaluateIndex,
            StringComparison.Ordinal);
        Assert.True(roomEvaluateIndex > evaluateIndex);
        string departmentDeleteAuthority = source[evaluateIndex..roomEvaluateIndex];
        Assert.DoesNotContain("ExistsAsync", departmentDeleteAuthority, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyIssued", departmentDeleteAuthority, StringComparison.Ordinal);
    }

    [Fact]
    public void WebDepartmentEditPageExists()
    {
        string editPath = Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml");
        Assert.True(File.Exists(editPath));

        string index = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/Pages/Administration/Departments/Index.cshtml"));
        Assert.Contains("Capabilities.CanEdit", index, StringComparison.Ordinal);
        Assert.Contains(">Edit<", index, StringComparison.Ordinal);
        Assert.Contains("./Edit", index, StringComparison.Ordinal);

        string editView = File.ReadAllText(editPath);
        string editCode = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml.cs"));
        Assert.Contains("IUpdateDepartmentCodeUseCase", editCode, StringComparison.Ordinal);
        Assert.Contains("IConfigurationLifecycleUseCase", editCode, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"DepartmentId\"", editView, StringComparison.Ordinal);
        Assert.Contains("type=\"hidden\"", editView, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"DepartmentCode\"", editView, StringComparison.Ordinal);
        Assert.Contains("is not editable", editView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<input asp-for=\"DepartmentId\"", editView, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "Key=OLD-KEY; WorkforceMember=WM-1; Justification=Department/FACILITIES",
        "Department",
        "FACILITIES")]
    [InlineData(
        "KEY#=SNAP-KEY; MEDECO=01; KeyAssetId=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee; WorkforceMember=WM-2; Justification=Room/101",
        "Room",
        "101")]
    public void MigrationProvenanceExtractParsesGen1AndGen2(
        string details,
        string expectedKind,
        string expectedCode)
    {
        Assert.True(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            details,
            out string kind,
            out string code));
        Assert.Equal(expectedKind, kind);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("KEY#=X; WorkforceMember=WM; Justification=Department/A/B")]
    [InlineData("KEY#=X; WorkforceMember=WM; Justification=Department/A;B")]
    [InlineData("KEY#=X; WorkforceMember=WM; NoJustificationHere")]
    [InlineData("")]
    public void MigrationProvenanceExtractRejectsAmbiguousOrMissing(string details)
    {
        Assert.False(KeyIssuedJustificationProvenanceExtract.TryParseJustificationSegment(
            details,
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

using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Workflow;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class LoanVerticalWorkflowTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider? _services;
    private bool _disposed;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync().ConfigureAwait(true);

        ServiceCollection services = new();
        services.AddDbContext<KeyInventoryDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IKeyCatalogPersistencePort, KeyCatalogPersistenceAdapter>();
        services.AddScoped<ILoanPersistencePort, LoanPersistenceAdapter>();
        services.AddScoped<ICreateKeyAssetUseCase, CreateKeyAssetUseCase>();
        services.AddScoped<IListKeyAssetsUseCase, ListKeyAssetsUseCase>();
        services.AddScoped<IIssueLoanUseCase, IssueLoanUseCase>();
        services.AddScoped<ICompleteReturnUseCase, CompleteReturnUseCase>();
        services.AddScoped<IListOpenLoansUseCase, ListOpenLoansUseCase>();
        services.AddScoped<IListReturnedLoansUseCase, ListReturnedLoansUseCase>();
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync().ConfigureAwait(true);
            _services = null;
        }

        await _connection.DisposeAsync().ConfigureAwait(true);
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _services?.Dispose();
        _connection.Dispose();
        _disposed = true;
    }

    [Fact]
    public async Task CreateKeyAssetSucceedsForNewTypeAndCatalogCode()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase create = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IListKeyAssetsUseCase list = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();

        await create.ExecuteAsync("key-100", "mechanical", CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> keys = await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(keys, key => key.CatalogKeyCode == "key-100" && key.TypeCode == "mechanical");
    }

    [Fact]
    public async Task IssueLoanSucceedsForExistingKeyAndRejectsNonUtcTimestamp()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase create = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        await create.ExecuteAsync("key-200", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset due = issued.AddDays(1);
        await issue.ExecuteAsync("loan-200", "key-200", "party-9", issued, due, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> openLoans = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(openLoans, loan => loan.LoanCode == "loan-200");

        DateTimeOffset nonUtc = new(2026, 8, 3, 12, 0, 0, TimeSpan.FromHours(-5));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            issue.ExecuteAsync("loan-201", "key-200", "party-9", nonUtc, nonUtc.AddDays(1), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteReturnSucceedsForOpenLoanAndRejectsNonOpenLoan()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase create = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListReturnedLoansUseCase listReturned = scope.ServiceProvider.GetRequiredService<IListReturnedLoansUseCase>();

        await create.ExecuteAsync("key-300", "mechanical", CancellationToken.None).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync("loan-300", "key-300", "party-3", issued, issued.AddDays(1), CancellationToken.None)
            .ConfigureAwait(true);

        await completeReturn.ExecuteAsync("return-300", "loan-300", issued.AddHours(2), CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> returned = await listReturned.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(returned, loan => loan.LoanCode == "loan-300" && loan.ReturnedAtUtc == issued.AddHours(2));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            completeReturn.ExecuteAsync("return-301", "loan-300", issued.AddHours(3), CancellationToken.None));
    }

    [Fact]
    public async Task ListOpenAndReturnedLoansReturnExpectedResults()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase create = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();
        IListReturnedLoansUseCase listReturned = scope.ServiceProvider.GetRequiredService<IListReturnedLoansUseCase>();

        await create.ExecuteAsync("key-400", "mechanical", CancellationToken.None).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync("loan-open", "key-400", "party-1", issued, issued.AddDays(1), CancellationToken.None)
            .ConfigureAwait(true);
        await create.ExecuteAsync("key-401", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await issue.ExecuteAsync("loan-done", "key-401", "party-2", issued, issued.AddDays(1), CancellationToken.None)
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

using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.Infrastructure;

public static class LoanVerticalComposition
{
    public static void AddLoanVertical(IServiceCollection services, string sqliteConnectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteConnectionString);

        services.AddDbContext<KeyInventoryDbContext>(options => options.UseSqlite(sqliteConnectionString));
        services.AddScoped<IKeyCatalogPersistencePort, KeyCatalogPersistenceAdapter>();
        services.AddScoped<ILoanPersistencePort, LoanPersistenceAdapter>();
        services.AddScoped<ICreateKeyAssetUseCase, CreateKeyAssetUseCase>();
        services.AddScoped<IListKeyAssetsUseCase, ListKeyAssetsUseCase>();
        services.AddScoped<IIssueLoanUseCase, IssueLoanUseCase>();
        services.AddScoped<ICompleteReturnUseCase, CompleteReturnUseCase>();
        services.AddScoped<IListOpenLoansUseCase, ListOpenLoansUseCase>();
        services.AddScoped<IListReturnedLoansUseCase, ListReturnedLoansUseCase>();
    }
}

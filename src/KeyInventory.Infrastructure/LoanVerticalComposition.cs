using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Lookup;
using KeyInventory.Infrastructure.Workforce;
using KeyInventory.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.Infrastructure;

public static class LoanVerticalComposition
{
    public static void AddLoanVertical(IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<KeyInventoryDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IKeyCatalogPersistencePort, KeyCatalogPersistenceAdapter>();
        services.AddScoped<ILoanPersistencePort, LoanPersistenceAdapter>();
        services.AddScoped<IWorkforcePersistencePort, WorkforcePersistenceAdapter>();
        services.AddScoped<ICreateKeyAssetUseCase, CreateKeyAssetUseCase>();
        services.AddScoped<IListKeyAssetsUseCase, ListKeyAssetsUseCase>();
        services.AddScoped<IIssueLoanUseCase, IssueLoanUseCase>();
        services.AddScoped<ICompleteReturnUseCase, CompleteReturnUseCase>();
        services.AddScoped<IListOpenLoansUseCase, ListOpenLoansUseCase>();
        services.AddScoped<IListReturnedLoansUseCase, ListReturnedLoansUseCase>();
        services.AddScoped<ICreatePartyUseCase, CreatePartyUseCase>();
        services.AddScoped<ICreateOrganizationUseCase, CreateOrganizationUseCase>();
        services.AddScoped<IListOrganizationsUseCase, ListOrganizationsUseCase>();
        services.AddScoped<ICreateDepartmentUseCase, CreateDepartmentUseCase>();
        services.AddScoped<IListDepartmentsUseCase, ListDepartmentsUseCase>();
        services.AddScoped<ICreateBuildingUseCase, CreateBuildingUseCase>();
        services.AddScoped<IListBuildingsUseCase, ListBuildingsUseCase>();
        services.AddScoped<ICreateRoomUseCase, CreateRoomUseCase>();
        services.AddScoped<IListRoomsUseCase, ListRoomsUseCase>();
        services.AddScoped<ICreateWorkforceMemberUseCase, CreateWorkforceMemberUseCase>();
        services.AddScoped<ICreateBootstrapWorkforcePairUseCase, CreateBootstrapWorkforcePairUseCase>();
        services.AddScoped<IListWorkforceMembersUseCase, ListWorkforceMembersUseCase>();
        services.AddScoped<ITerminateWorkforceMemberUseCase, TerminateWorkforceMemberUseCase>();
        services.AddScoped<ICreateWorkAssignmentUseCase, CreateWorkAssignmentUseCase>();
        services.AddScoped<IListWorkAssignmentsUseCase, ListWorkAssignmentsUseCase>();
        services.AddScoped<IListOutstandingReturnObligationsUseCase, ListOutstandingReturnObligationsUseCase>();
        services.AddScoped<IOperationalKeyLookupPort, OperationalKeyLookupAdapter>();
        services.AddScoped<IOperationalKeyLookupUseCase, OperationalKeyLookupUseCase>();
    }
}

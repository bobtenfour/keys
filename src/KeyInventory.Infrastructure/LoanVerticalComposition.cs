using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Readiness;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure.Catalog;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Lookup;
using KeyInventory.Infrastructure.OperatorAudit;
using KeyInventory.Infrastructure.Reports;
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
        services.AddHttpContextAccessor();
        services.AddScoped<IOperatorIdentityAccessor, OperatorIdentityAccessor>();
        services.AddScoped<IOperatorAuditPersistencePort, OperatorAuditPersistenceAdapter>();
        services.AddScoped<IOperatorAuditRecorder, OperatorAuditRecorder>();
        services.AddScoped<IOperatorAuditTrailUseCase, OperatorAuditTrailUseCase>();
        services.AddScoped<IKeyRoomAssignmentPersistencePort, KeyRoomAssignmentPersistenceAdapter>();
        services.AddScoped<IKeyCatalogPersistencePort, KeyCatalogPersistenceAdapter>();
        services.AddScoped<ILoanPersistencePort, LoanPersistenceAdapter>();
        services.AddScoped<IWorkforcePersistencePort, WorkforcePersistenceAdapter>();
        services.AddScoped<ICreateKeyAssetUseCase, CreateKeyAssetUseCase>();
        services.AddScoped<IListKeyAssetsUseCase, ListKeyAssetsUseCase>();
        services.AddScoped<IKeyRoomAssignmentUseCase, KeyRoomAssignmentUseCase>();
        services.AddScoped<IIssueLoanUseCase, IssueLoanUseCase>();
        services.AddScoped<ICompleteReturnUseCase, CompleteReturnUseCase>();
        services.AddScoped<IListOpenLoansUseCase, ListOpenLoansUseCase>();
        services.AddScoped<IListReturnedLoansUseCase, ListReturnedLoansUseCase>();
        services.AddScoped<ICreatePartyUseCase, CreatePartyUseCase>();
        services.AddScoped<IListPartiesUseCase, ListPartiesUseCase>();
        services.AddScoped<ICreateDepartmentUseCase, CreateDepartmentUseCase>();
        services.AddScoped<IListDepartmentsUseCase, ListDepartmentsUseCase>();
        services.AddScoped<IActivateDepartmentUseCase, ActivateDepartmentUseCase>();
        services.AddScoped<IRetireDepartmentUseCase, RetireDepartmentUseCase>();
        services.AddScoped<ICreateRoomUseCase, CreateRoomUseCase>();
        services.AddScoped<IListRoomsUseCase, ListRoomsUseCase>();
        services.AddScoped<IActivateRoomUseCase, ActivateRoomUseCase>();
        services.AddScoped<IRetireRoomUseCase, RetireRoomUseCase>();
        services.AddScoped<IUpdateRoomNumberUseCase, UpdateRoomNumberUseCase>();
        services.AddScoped<IUpdateRoomDescriptionUseCase, UpdateRoomDescriptionUseCase>();
        services.AddScoped<IUpdatePartyNameUseCase, UpdatePartyNameUseCase>();
        services.AddScoped<ICorrectPartyUinUseCase, CorrectPartyUinUseCase>();
        services.AddScoped<IListKeyTypesUseCase, ListKeyTypesUseCase>();
        services.AddScoped<IActivateKeyTypeUseCase, ActivateKeyTypeUseCase>();
        services.AddScoped<IRetireKeyTypeUseCase, RetireKeyTypeUseCase>();
        services.AddScoped<ICreateWorkforceMemberUseCase, CreateWorkforceMemberUseCase>();
        services.AddScoped<IRegisterWorkforceMemberUseCase, RegisterWorkforceMemberUseCase>();
        services.AddScoped<IListWorkforceMembersUseCase, ListWorkforceMembersUseCase>();
        services.AddScoped<ITerminateWorkforceMemberUseCase, TerminateWorkforceMemberUseCase>();
        services.AddScoped<IUpdateWorkforceMemberDepartmentUseCase, UpdateWorkforceMemberDepartmentUseCase>();
        services.AddScoped<IUpdateWorkforceMemberWorkforceTypeUseCase, UpdateWorkforceMemberWorkforceTypeUseCase>();
        services.AddScoped<ICreateWorkAssignmentUseCase, CreateWorkAssignmentUseCase>();
        services.AddScoped<IListWorkAssignmentsUseCase, ListWorkAssignmentsUseCase>();
        services.AddScoped<IEndWorkAssignmentUseCase, EndWorkAssignmentUseCase>();
        services.AddScoped<IMarkWorkAssignmentPrimaryUseCase, MarkWorkAssignmentPrimaryUseCase>();
        services.AddScoped<IClearWorkAssignmentPrimaryUseCase, ClearWorkAssignmentPrimaryUseCase>();
        services.AddScoped<IListOutstandingReturnObligationsUseCase, ListOutstandingReturnObligationsUseCase>();
        services.AddScoped<IOperationalReadinessUseCase, OperationalReadinessUseCase>();
        services.AddScoped<IOperationalKeyLookupPort, OperationalKeyLookupAdapter>();
        services.AddScoped<IOperationalKeyLookupUseCase, OperationalKeyLookupUseCase>();
        services.AddScoped<IOperationalReportsPort, OperationalReportsAdapter>();
        services.AddSingleton<IReportExcelExporter, ClosedXmlReportExcelExporter>();
        services.AddSingleton<IReportPdfExporter, QuestPdfReportPdfExporter>();
        services.AddScoped<IOperationalReportsUseCase, OperationalReportsUseCase>();
    }
}

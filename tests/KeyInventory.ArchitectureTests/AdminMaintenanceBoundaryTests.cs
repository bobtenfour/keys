using System.Reflection;
using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Web.Pages.Administration.Departments;
using KeyInventory.Web.Pages.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class AdminMaintenanceBoundaryTests
{
    [Fact]
    public void AdministrationAndCatalogMaintenancePagesDoNotBypassApplication()
    {
        Type[] pageModels =
        [
            typeof(IndexModel),
            typeof(KeyInventory.Web.Pages.Administration.Rooms.IndexModel),
            typeof(KeyInventory.Web.Pages.Administration.WorkforceMembers.IndexModel),
            typeof(KeyInventory.Web.Pages.Administration.WorkAssignments.IndexModel),
            typeof(KeysModel),
            typeof(DeleteModel),
            typeof(KeyInventory.Web.Pages.Administration.Rooms.DeleteModel),
            typeof(KeyInventory.Web.Pages.Administration.WorkforceMembers.DeleteModel),
            typeof(KeyInventory.Web.Pages.Administration.WorkAssignments.DeleteModel),
            typeof(KeyInventory.Web.Pages.Catalog.Keys.DeleteModel),
            typeof(KeyInventory.Web.Pages.Catalog.Keys.DeletePatternModel)
        ];

        foreach (Type pageModel in pageModels)
        {
            Assert.DoesNotContain(
                pageModel.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter =>
                    parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal)
                    || (parameter.ParameterType.Namespace?.StartsWith("KeyInventory.Infrastructure", StringComparison.Ordinal) ?? false)
                    || parameter.ParameterType == typeof(IWorkforcePersistencePort)
                    || parameter.ParameterType == typeof(IKeyCatalogPersistencePort)
                    || parameter.ParameterType == typeof(ILoanPersistencePort)
                    || parameter.ParameterType == typeof(IOperatorAuditPersistencePort));
        }

        Assert.Contains(
            typeof(IndexModel).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IConfigurationLifecycleUseCase));
        Assert.Contains(
            typeof(KeysModel).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IConfigurationLifecycleUseCase));
    }

    [Fact]
    public void SliceDoesNotIntroduceGenericCrudFrameworkOrHistoryRewriteStores()
    {
        Assembly[] assemblies =
        [
            typeof(IActivateDepartmentUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "GenericRepository",
                "CrudController",
                "CrudService",
                "AdminHistory",
                "VersionStore",
                "ArchiveStore",
                "Reports2",
                "Sqlite",
                "InMemory"))
            .ToArray();

        Assert.Empty(prohibited);

        // Permanent delete of unreferenced configuration records is Application-owned via
        // IConfigurationLifecycleUseCase; raw ports may expose Restrict delete helpers.
        Assert.Contains(
            typeof(IConfigurationLifecycleUseCase).GetMethods().Select(method => method.Name),
            name => name.StartsWith("Delete", StringComparison.Ordinal));
    }

    [Fact]
    public void CompositionRegistersMaintenanceAndLifecycleUseCases()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration["ConnectionStrings:KeyInventory"] = KeyInventorySqlServerTestConnection.Require();
        KeyInventory.Web.WebServiceComposition.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IActivateDepartmentUseCase>());
        Assert.NotNull(provider.GetService<IRetireDepartmentUseCase>());
        Assert.NotNull(provider.GetService<IUpdateRoomNumberUseCase>());
        Assert.NotNull(provider.GetService<IRetireRoomUseCase>());
        Assert.NotNull(provider.GetService<IUpdateWorkforceMemberDepartmentUseCase>());
        Assert.NotNull(provider.GetService<IUpdatePartyNameUseCase>());
        Assert.NotNull(provider.GetService<ICorrectPartyUinUseCase>());
        Assert.NotNull(provider.GetService<IEndWorkAssignmentUseCase>());
        Assert.NotNull(provider.GetService<ITerminateWorkforceMemberUseCase>());
        Assert.NotNull(provider.GetService<IConfigurationLifecycleUseCase>());
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

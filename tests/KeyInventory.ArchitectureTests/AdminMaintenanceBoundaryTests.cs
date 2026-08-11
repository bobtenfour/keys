using System.Reflection;
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
            typeof(KeyTypesModel)
        ];

        foreach (Type pageModel in pageModels)
        {
            Assert.DoesNotContain(
                pageModel.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter =>
                    parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal)
                    || (parameter.ParameterType.Namespace?.StartsWith("KeyInventory.Infrastructure", StringComparison.Ordinal) ?? false)
                    || parameter.ParameterType == typeof(IWorkforcePersistencePort)
                    || parameter.ParameterType == typeof(IKeyCatalogPersistencePort));
        }

        Assert.Contains(
            typeof(IndexModel).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IActivateDepartmentUseCase));
        Assert.Contains(
            typeof(KeyTypesModel).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IRetireKeyTypeUseCase));
    }

    [Fact]
    public void SliceDoesNotIntroduceHardDeleteGenericCrudOrHistoryFramework()
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
                "HardDelete",
                "PhysicalDelete",
                "AdminHistory",
                "VersionStore",
                "ArchiveStore",
                "Reports2",
                "Sqlite",
                "InMemory"))
            .ToArray();

        Assert.Empty(prohibited);

        string[] deleteMethods = typeof(IWorkforcePersistencePort)
            .GetMethods()
            .Concat(typeof(IKeyCatalogPersistencePort).GetMethods())
            .Select(method => method.Name)
            .Where(name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Remove", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(deleteMethods);
    }

    [Fact]
    public void CompositionRegistersMaintenanceUseCases()
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
        Assert.NotNull(provider.GetService<IActivateKeyTypeUseCase>());
        Assert.NotNull(provider.GetService<IRetireKeyTypeUseCase>());
        Assert.NotNull(provider.GetService<IUpdateWorkforceMemberDepartmentUseCase>());
        Assert.NotNull(provider.GetService<IUpdatePartyNameUseCase>());
        Assert.NotNull(provider.GetService<ICorrectPartyUinUseCase>());
        Assert.NotNull(provider.GetService<IEndWorkAssignmentUseCase>());
        Assert.NotNull(provider.GetService<IMarkWorkAssignmentPrimaryUseCase>());
        Assert.NotNull(provider.GetService<ITerminateWorkforceMemberUseCase>());
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

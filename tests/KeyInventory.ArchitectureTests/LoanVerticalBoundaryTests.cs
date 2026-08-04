using System.Reflection;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class LoanVerticalBoundaryTests
{
    [Fact]
    public void WebDoesNotReferenceDomainAggregatesForBusinessDecisions()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] domainUsages = webAssembly
            .GetTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(property => property.PropertyType.FullName ?? property.PropertyType.Name)
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => field.FieldType.FullName ?? field.FieldType.Name)))
            .Where(name => name.StartsWith("KeyInventory.Domain.", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(domainUsages);
    }

    [Fact]
    public void InfrastructureAdaptersDoNotDefineDomainInvariantMethodsBeyondMapping()
    {
        Assembly infrastructureAssembly = typeof(LoanVerticalComposition).Assembly;

        string[] prohibitedMethods = infrastructureAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Infrastructure.Workflow", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Select(method => method.Name)
            .Where(name => ContainsAny(name, "Cancel", "Retire", "Activate", "MarkReturned", "AssignRole"))
            .ToArray();

        Assert.Empty(prohibitedMethods);
    }

    [Fact]
    public void SliceDoesNotIntroduceAlternateAuthProvidersOrAuditEmissionTypes()
    {
        Assembly[] assemblies =
        [
            typeof(ICreateKeyAssetUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "JwtBearer",
                "OpenIdConnect",
                "AuditEmission",
                "AuditEmitter"))
            .ToArray();

        Assert.Empty(prohibited);
        Assert.NotNull(typeof(KeyInventory.Web.Program).Assembly.GetType(
            "KeyInventory.Web.Authorization.LocalBootstrapAdminSeeder"));
    }

    [Fact]
    public void CompositionRegistersLoanVerticalUseCases()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Configuration["ConnectionStrings:KeyInventory"] = KeyInventorySqlServerTestConnection.Require();
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        KeyInventory.Web.WebServiceComposition.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        using WebApplication app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<ICreateKeyAssetUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IIssueLoanUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICompleteReturnUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IListOpenLoansUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IListReturnedLoansUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IListKeyAssetsUseCase>());
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

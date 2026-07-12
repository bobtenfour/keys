using System.Reflection;
using KeyInventory.Application.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class CatalogBoundaryTests
{
    [Fact]
    public void CatalogDomainDoesNotOwnFutureSliceAuthority()
    {
        Assembly domainAssembly = typeof(KeyInventory.Domain.Catalog.KeyAsset).Assembly;

        string[] catalogTypeNames = domainAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Domain.Catalog", StringComparison.Ordinal))
            .Select(type => type.Name)
            .Where(name => ContainsAny(
                name,
                "Loan",
                "Return",
                "Custody",
                "Audit",
                "Lifecycle",
                "Maintenance",
                "Inventory",
                "Policy",
                "Authentication",
                "Authorization",
                "Permission",
                "Role",
                "Principal",
                "Party"))
            .ToArray();

        Assert.Empty(catalogTypeNames);
    }

    [Fact]
    public void KeyAssetDoesNotExposeProhibitedAuthorityState()
    {
        PropertyInfo[] properties = typeof(KeyInventory.Domain.Catalog.KeyAsset).GetProperties();

        string[] prohibitedProperties = properties
            .Select(property => property.Name)
            .Where(name => ContainsAny(
                name,
                "Possession",
                "Custodian",
                "Loan",
                "Return",
                "Lifecycle",
                "Audit",
                "Maintenance",
                "Authorization",
                "Authentication",
                "Policy",
                "Ui"))
            .ToArray();

        Assert.Empty(prohibitedProperties);
    }

    [Fact]
    public void ApplicationDefinesLookupPortsOnlyForCatalog()
    {
        Assembly applicationAssembly = typeof(IKeyAssetLookupPort).Assembly;

        Type[] catalogTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Catalog", StringComparison.Ordinal))
            .ToArray();

        Assert.All(catalogTypes, type =>
        {
            Assert.True(type.IsInterface);
            Assert.EndsWith("LookupPort", type.Name, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ApplicationCatalogPortsDoNotIntroduceCommandsOrProviders()
    {
        Assembly applicationAssembly = typeof(IKeyAssetLookupPort).Assembly;

        string[] prohibitedTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Catalog", StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "Command",
                "Repository",
                "Service",
                "Provider",
                "Sql",
                "EntityFramework",
                "DbContext",
                "Configuration"))
            .ToArray();

        Assert.Empty(prohibitedTypes);
    }

    [Fact]
    public void InfrastructureDoesNotOwnCatalogImplementations()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] catalogTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "KeyAsset", "KeySeries", "KeyType", "Lock", "Location"))
            .ToArray();

        Assert.Empty(catalogTypes);
    }

    [Fact]
    public void WebContainsNoCatalogBusinessTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] catalogTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "KeyAsset", "KeySeries", "KeyType", "Lock", "Location"))
            .ToArray();

        Assert.Empty(catalogTypes);
    }

    [Fact]
    public void WebServiceProviderDoesNotRegisterCatalogLookupPorts()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        KeyInventory.Web.WebServiceComposition.Configure(builder.Services);

        using WebApplication app = builder.Build();

        using IServiceScope scope = app.Services.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<IKeyAssetLookupPort>());
        Assert.Null(scope.ServiceProvider.GetService<IKeySeriesLookupPort>());
        Assert.Null(scope.ServiceProvider.GetService<IKeyTypeLookupPort>());
        Assert.Null(scope.ServiceProvider.GetService<ILockLookupPort>());
        Assert.Null(scope.ServiceProvider.GetService<ILocationLookupPort>());
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

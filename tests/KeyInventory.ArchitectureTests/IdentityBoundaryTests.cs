using System.Reflection;
using KeyInventory.Application.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class IdentityBoundaryTests
{
    [Fact]
    public void IdentityDomainDoesNotIntroduceAuthenticationProviderTypes()
    {
        Assembly domainAssembly = typeof(KeyInventory.Domain.Identity.SecurityPrincipal).Assembly;

        string[] typeNames = domainAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Password", "Jwt", "Cookie", "OAuth", "OpenId", "AspNetIdentity", "Login", "Logout"))
            .ToArray();

        Assert.Empty(typeNames);
    }

    [Fact]
    public void IdentityDomainDoesNotOwnPartyEntity()
    {
        Assembly domainAssembly = typeof(KeyInventory.Domain.Identity.SecurityPrincipal).Assembly;

        Type? partyType = domainAssembly
            .GetTypes()
            .SingleOrDefault(type => string.Equals(type.Name, "Party", StringComparison.Ordinal));

        Assert.Null(partyType);
    }

    [Fact]
    public void WebContainsNoIdentityBusinessTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] identityBusinessTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "SecurityPrincipal", "RolePermission", "PrincipalRoleAssignment"))
            .ToArray();

        Assert.Empty(identityBusinessTypes);
    }

    [Fact]
    public void WebServiceProviderValidatesIdentityComposition()
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
        Assert.Null(scope.ServiceProvider.GetService<IIdentityPrincipalService>());
        Assert.Null(scope.ServiceProvider.GetService<IRoleAssignmentService>());
        Assert.Null(scope.ServiceProvider.GetService<IRolePermissionService>());
    }

    [Fact]
    public void InfrastructureDoesNotOwnIdentityOrRbacDomainDefinitions()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] identityDomainTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "SecurityPrincipal",
                "RolePermission",
                "PrincipalRoleAssignment",
                "AuthorizationScopeType"))
            .ToArray();

        Assert.Empty(identityDomainTypes);
    }

    [Fact]
    public void InfrastructureDoesNotImplementIdentityPersistenceInThisSlice()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] repositoryTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Repository", "Store", "Persistence"))
            .ToArray();

        Assert.Empty(repositoryTypes);
    }

    [Fact]
    public void ApplicationOwnsIdentityPortsOnly()
    {
        Assembly applicationAssembly = typeof(IIdentityPrincipalService).Assembly;

        string[] providerTypes = applicationAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Sql", "EntityFramework", "DbContext", "Jwt", "Cookie", "OAuth", "OpenId"))
            .ToArray();

        Assert.Empty(providerTypes);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

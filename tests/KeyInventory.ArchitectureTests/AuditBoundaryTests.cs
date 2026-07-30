using System.Reflection;
using KeyInventory.Application.Audit;
using KeyInventory.Domain.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class AuditBoundaryTests
{
    [Fact]
    public void AuditEventDoesNotExposeProhibitedAuthorityState()
    {
        AssertNoProhibitedMembers(typeof(AuditEvent));
    }

    [Fact]
    public void AuditDomainNamespaceDoesNotOwnForeignAuthorityTypes()
    {
        Assembly domainAssembly = typeof(AuditEvent).Assembly;

        string[] prohibitedTypeNames = domainAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Domain.Audit", StringComparison.Ordinal))
            .Select(type => type.Name)
            .Where(name => ContainsAny(
                name,
                "Custody",
                "Lifecycle",
                "Maintenance",
                "Inventory",
                "Policy",
                "Authentication",
                "Authorization",
                "Permission",
                "Role",
                "Party",
                "KeyType",
                "KeySeries",
                "Location",
                "Lock",
                "Loan",
                "Return",
                "DigitalTrust",
                "Hash",
                "Signature"))
            .ToArray();

        Assert.Empty(prohibitedTypeNames);
    }

    [Fact]
    public void ApplicationDefinesLookupPortOnlyForAuditEvent()
    {
        Assembly applicationAssembly = typeof(IAuditEventLookupPort).Assembly;

        Type[] auditTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Audit", StringComparison.Ordinal))
            .ToArray();

        Assert.Single(auditTypes);
        Assert.True(auditTypes[0].IsInterface);
        Assert.Equal(nameof(IAuditEventLookupPort), auditTypes[0].Name);
    }

    [Fact]
    public void ApplicationAuditPortsDoNotIntroduceCommandsServicesOrProviders()
    {
        Assembly applicationAssembly = typeof(IAuditEventLookupPort).Assembly;

        string[] prohibitedTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Audit", StringComparison.Ordinal))
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
                "Configuration",
                "Handler"))
            .ToArray();

        Assert.Empty(prohibitedTypes);
    }

    [Fact]
    public void InfrastructureDoesNotOwnAuditImplementations()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] auditTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Audit"))
            .ToArray();

        Assert.Empty(auditTypes);
    }

    [Fact]
    public void WebContainsNoAuditBusinessTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] auditTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Audit"))
            .ToArray();

        Assert.Empty(auditTypes);
    }

    [Fact]
    public void WebServiceProviderDoesNotRegisterAuditEventLookupPort()
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
        Assert.Null(scope.ServiceProvider.GetService<IAuditEventLookupPort>());
    }

    private static void AssertNoProhibitedMembers(Type type)
    {
        const BindingFlags declared =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        string[] prohibited = type
            .GetMembers(declared)
            .Select(member => member.Name)
            .Where(name => ContainsAny(
                name,
                "Possession",
                "Custodian",
                "Custody",
                "Lifecycle",
                "Authentication",
                "Authorization",
                "Policy",
                "Permission",
                "Role",
                "PartyProfile",
                "CatalogAuthority",
                "DigitalTrust",
                "HashChain",
                "Signature",
                "Credential",
                "Ui"))
            .Where(name => !string.Equals(name, "ActingSecurityPrincipal", StringComparison.Ordinal)
                && !string.Equals(name, "PartyReference", StringComparison.Ordinal)
                && !name.StartsWith("Subject", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(prohibited);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

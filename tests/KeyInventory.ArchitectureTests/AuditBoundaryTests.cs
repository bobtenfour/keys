using System.Reflection;
using KeyInventory.Application.Audit;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Audit;
using KeyInventory.Infrastructure.OperatorAudit;
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
    public void ApplicationAuditEventNamespaceRemainsLookupOnly()
    {
        Assembly applicationAssembly = typeof(IAuditEventLookupPort).Assembly;

        Type[] auditEventTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Audit", StringComparison.Ordinal))
            .ToArray();

        Assert.Single(auditEventTypes);
        Assert.True(auditEventTypes[0].IsInterface);
        Assert.Equal(nameof(IAuditEventLookupPort), auditEventTypes[0].Name);
    }

    [Fact]
    public void ApplicationDefinesOperatorAuditAuthority()
    {
        Assert.True(typeof(IOperatorAuditRecorder).IsInterface);
        Assert.True(typeof(IOperatorAuditPersistencePort).IsInterface);
        Assert.True(typeof(IOperatorAuditTrailUseCase).IsInterface);
        Assert.Contains(
            typeof(IOperatorAuditPersistencePort).GetMethods(),
            method => method.Name == nameof(IOperatorAuditPersistencePort.Stage));
        Assert.DoesNotContain(
            typeof(IOperatorAuditPersistencePort).GetMethods(),
            method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("Update", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InfrastructureOwnsOperatorAuditPersistenceOnly()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] auditTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains("Audit", StringComparison.OrdinalIgnoreCase))
            // Migration designer/snapshot and one-time provenance extract are not OperatorAudit persistence types.
            .Where(name => !name.Contains(".Migrations.", StringComparison.Ordinal))
            .Where(name => !name.Contains("ProvenanceExtract", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(auditTypes);
        Assert.All(auditTypes, name => Assert.Contains("OperatorAudit", name, StringComparison.Ordinal));
        Assert.Contains(auditTypes, name => name.Contains(nameof(OperatorAuditPersistenceAdapter), StringComparison.Ordinal));
        Assert.DoesNotContain(auditTypes, name => name.Contains("AuditEvent", StringComparison.Ordinal));
    }

    [Fact]
    public void WebAuditTrailIsPresentationOnly()
    {
        string code = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/Pages/Administration/AuditTrail.cshtml.cs"));
        Assert.Contains("IOperatorAuditTrailUseCase", code, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperatorAuditPersistencePort", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Stage(", code, StringComparison.Ordinal);
    }

    [Fact]
    public void WebServiceProviderRegistersOperatorAuditNotDomainAuditEventLookup()
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

        builder.Configuration["ConnectionStrings:KeyInventory"] = KeyInventorySqlServerTestConnection.Require();
        KeyInventory.Web.WebServiceComposition.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        using WebApplication app = builder.Build();

        using IServiceScope scope = app.Services.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<IAuditEventLookupPort>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOperatorAuditTrailUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOperatorAuditRecorder>());
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

    private static string RepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}

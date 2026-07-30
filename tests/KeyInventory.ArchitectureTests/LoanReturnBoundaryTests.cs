using System.Reflection;
using KeyInventory.Application.Loans;
using KeyInventory.Domain.Loans;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class LoanReturnBoundaryTests
{
    [Fact]
    public void LoanAndReturnDoNotExposeProhibitedAuthorityState()
    {
        AssertNoProhibitedMembers(typeof(Loan));
        AssertNoProhibitedMembers(typeof(Return));
        AssertNoProhibitedMembers(typeof(LoanStatus));
    }

    [Fact]
    public void LoanDomainNamespaceDoesNotOwnForeignAuthorityTypes()
    {
        Assembly domainAssembly = typeof(Loan).Assembly;

        string[] prohibitedTypeNames = domainAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Domain.Loans", StringComparison.Ordinal))
            .Select(type => type.Name)
            .Where(name => ContainsAny(
                name,
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
                "Party",
                "KeyType",
                "KeySeries",
                "Location",
                "Lock"))
            .ToArray();

        Assert.Empty(prohibitedTypeNames);
    }

    [Fact]
    public void ApplicationDefinesLookupPortsOnlyForLoanAndReturn()
    {
        Assembly applicationAssembly = typeof(ILoanLookupPort).Assembly;

        Type[] loanTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Loans", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(2, loanTypes.Length);
        Assert.All(loanTypes, type =>
        {
            Assert.True(type.IsInterface);
            Assert.EndsWith("LookupPort", type.Name, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ApplicationLoanPortsDoNotIntroduceCommandsServicesOrProviders()
    {
        Assembly applicationAssembly = typeof(ILoanLookupPort).Assembly;

        string[] prohibitedTypes = applicationAssembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "KeyInventory.Application.Loans", StringComparison.Ordinal))
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
    public void InfrastructureDoesNotOwnLoanOrReturnImplementations()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] loanTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Loan", "Return"))
            .ToArray();

        Assert.Empty(loanTypes);
    }

    [Fact]
    public void WebContainsNoLoanOrReturnBusinessTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] loanTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "Loan", "Return"))
            .ToArray();

        Assert.Empty(loanTypes);
    }

    [Fact]
    public void WebServiceProviderDoesNotRegisterLoanOrReturnLookupPorts()
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
        Assert.Null(scope.ServiceProvider.GetService<ILoanLookupPort>());
        Assert.Null(scope.ServiceProvider.GetService<IReturnLookupPort>());
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
                "Audit",
                "Authentication",
                "Authorization",
                "Policy",
                "Permission",
                "Role",
                "Principal",
                "PartyProfile",
                "CatalogAuthority",
                "Ui"))
            .ToArray();

        Assert.Empty(prohibited);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

using System.Reflection;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class WorkforceEligibilityBoundaryTests
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

        // Register page binds Regular/Master classification radios via the domain enum.
        Assert.All(
            domainUsages,
            name => Assert.Equal("KeyInventory.Domain.Catalog.KeyAccessClassification", name));
    }

    [Fact]
    public void WorkforceMemberDoesNotDuplicatePartyIdentityAttributes()
    {
        string[] propertyNames = typeof(WorkforceMember)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("FirstName", propertyNames);
        Assert.DoesNotContain("LastName", propertyNames);
        Assert.DoesNotContain("Uin", propertyNames);
        Assert.Contains(nameof(WorkforceMember.PartyCode), propertyNames);
        Assert.NotNull(typeof(Party).GetProperty(nameof(Party.FirstName)));
        Assert.NotNull(typeof(Party).GetProperty(nameof(Party.Uin)));
    }

    [Fact]
    public void SliceDoesNotIntroduceBorrowerAggregateOrForbiddenCapabilities()
    {
        Assembly[] assemblies =
        [
            typeof(ICreateDepartmentUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly,
            typeof(WorkforceMember).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "BorrowerAggregate",
                "TemporaryBorrower",
                "HrIntegration",
                "AuditEmission",
                "AuditEmitter",
                "CustodyMutation",
                "LifecycleMutation",
                "AutomaticOffboarding"))
            .ToArray();

        Assert.Empty(prohibited);
        Assert.Null(assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .SingleOrDefault(type => string.Equals(type.Name, "Borrower", StringComparison.Ordinal)));
    }

    [Fact]
    public void CompositionRegistersWorkforceUseCases()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Configuration["ConnectionStrings:KeyInventory"] =
            KeyInventorySqlServerTestConnection.Require();

        KeyInventory.Web.WebServiceComposition.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ICreateDepartmentUseCase>());
        Assert.NotNull(provider.GetService<IRegisterWorkforceMemberUseCase>());
        Assert.NotNull(provider.GetService<IListOutstandingReturnObligationsUseCase>());
        Assert.NotNull(provider.GetService<ICreateWorkAssignmentUseCase>());
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

using System.Reflection;
using KeyInventory.Application.Lookup;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Web.Pages.Operations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyLookupBoundaryTests
{
    [Fact]
    public void OperatorPagesDoNotBypassApplicationWithDbContextAccess()
    {
        Type[] pageModels =
        [
            typeof(FindModel),
            typeof(MemberKeysModel),
            typeof(ActiveModel),
            typeof(HistoryModel),
            typeof(ReceiveModel),
            typeof(IssueModel),
            typeof(KeyInventory.Web.Pages.IndexModel)
        ];

        string[] persistenceUsages = pageModels
            .SelectMany(type => type.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
                .Concat(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(property => property.PropertyType.FullName ?? property.PropertyType.Name))
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => field.FieldType.FullName ?? field.FieldType.Name)))
            .Where(name =>
                name.Contains("KeyInventoryDbContext", StringComparison.Ordinal)
                || name.StartsWith("KeyInventory.Infrastructure.Data", StringComparison.Ordinal)
                || name.Contains("DbSet`", StringComparison.Ordinal)
                || name.Contains("IOperationalKeyLookupPort", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(persistenceUsages);
        Assert.All(
            pageModels.SelectMany(type => type.GetConstructors()).SelectMany(constructor => constructor.GetParameters()),
            parameter => Assert.False(
                parameter.ParameterType.Namespace?.StartsWith("KeyInventory.Infrastructure", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void FindAndMemberKeysPagesShareOperationalLookupUseCaseOnly()
    {
        ConstructorInfo findCtor = typeof(FindModel).GetConstructors().Single();
        ConstructorInfo memberCtor = typeof(MemberKeysModel).GetConstructors().Single();
        ConstructorInfo activeCtor = typeof(ActiveModel).GetConstructors().Single();
        ConstructorInfo historyCtor = typeof(HistoryModel).GetConstructors().Single();

        Assert.Contains(findCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IOperationalKeyLookupUseCase));
        Assert.Contains(memberCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IOperationalKeyLookupUseCase));
        Assert.Contains(activeCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IOperationalKeyLookupUseCase));
        Assert.Contains(historyCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IOperationalKeyLookupUseCase));

        Assert.DoesNotContain(findCtor.GetParameters(), parameter =>
            parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        Assert.Single(
            typeof(IOperationalKeyLookupUseCase).Assembly.GetTypes(),
            type => type.IsClass && !type.IsAbstract && typeof(IOperationalKeyLookupUseCase).IsAssignableFrom(type));
    }

    [Fact]
    public void SliceDoesNotIntroduceExternalSearchOrReportingInfrastructure()
    {
        Assembly[] assemblies =
        [
            typeof(IOperationalKeyLookupUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "Elasticsearch",
                "Lucene",
                "FuzzySearch",
                "ReportingStore",
                "ReportProjection",
                "SearchIndex",
                "Reports1"))
            .ToArray();

        Assert.Empty(prohibited);
    }

    [Fact]
    public void CompositionRegistersOperationalLookupAuthority()
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
        Assert.NotNull(provider.GetService<IOperationalKeyLookupUseCase>());
        Assert.NotNull(provider.GetService<IOperationalKeyLookupPort>());
        Assert.IsType<KeyInventory.Infrastructure.Lookup.OperationalKeyLookupAdapter>(
            provider.GetRequiredService<IOperationalKeyLookupPort>());
    }

    [Fact]
    public void SqlServerRemainsSolePersistenceProviderForLookupComposition()
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
        using IServiceScope scope = provider.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

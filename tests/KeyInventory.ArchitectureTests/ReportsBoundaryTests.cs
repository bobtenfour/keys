using System.Reflection;
using KeyInventory.Application.Reports;
using KeyInventory.Infrastructure;
using KeyInventory.Web.Pages.Reports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class ReportsBoundaryTests
{
    [Fact]
    public void ReportPagesConsumeApplicationUseCaseWithoutInfrastructureBypass()
    {
        Type[] pageModels =
        [
            typeof(CurrentKeyHoldersModel),
            typeof(ActiveLoansModel),
            typeof(OverdueKeysModel),
            typeof(KeysByWorkforceMemberModel),
            typeof(KeyHistoryModel),
            typeof(OutstandingByWorkforceStatusModel),
            typeof(KeyCatalogModel)
        ];

        foreach (Type pageModel in pageModels)
        {
            ConstructorInfo ctor = pageModel.GetConstructors().Single();
            Assert.Contains(ctor.GetParameters(), parameter => parameter.ParameterType == typeof(IOperationalReportsUseCase));
            Assert.DoesNotContain(
                ctor.GetParameters(),
                parameter =>
                    parameter.ParameterType == typeof(IOperationalReportsPort)
                    || (parameter.ParameterType.Namespace?.StartsWith("KeyInventory.Infrastructure", StringComparison.Ordinal) ?? false)
                    || parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void SliceDoesNotIntroduceBiWarehouseOrExportPlatformTypes()
    {
        Assembly[] assemblies =
        [
            typeof(IOperationalReportsUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "ExcelExport",
                "PdfExport",
                "DataWarehouse",
                "ReportDesigner",
                "Elasticsearch",
                "DashboardWidget",
                "Reports2",
                "ChartJs"))
            .ToArray();

        Assert.Empty(prohibited);
    }

    [Fact]
    public void CompositionRegistersReportsAuthorityAsReadOnlyUseCase()
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
        Assert.NotNull(provider.GetService<IOperationalReportsUseCase>());
        Assert.NotNull(provider.GetService<IOperationalReportsPort>());

        string[] mutatingNames = typeof(IOperationalReportsUseCase)
            .GetMethods()
            .Select(method => method.Name)
            .Where(name => ContainsAny(name, "Create", "Update", "Delete", "Terminate", "Issue", "Complete", "Add"))
            .ToArray();
        Assert.Empty(mutatingNames);
    }

    [Fact]
    public void CsvFormatterEscapesCommasQuotesAndNewLines()
    {
        string csv = ReportCsvFormatter.Build(
            ["Name", "Note"],
            [
                ["Ada Lovelace", "ok"],
                ["A,B", "line1\nline2"],
                ["Say \"Hi\"", "plain"]
            ]);

        Assert.Contains("\"A,B\"", csv, StringComparison.Ordinal);
        Assert.Contains("\"line1\nline2\"", csv, StringComparison.Ordinal);
        Assert.Contains("\"Say \"\"Hi\"\"\"", csv, StringComparison.Ordinal);
        Assert.StartsWith("Name,Note\n", csv, StringComparison.Ordinal);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

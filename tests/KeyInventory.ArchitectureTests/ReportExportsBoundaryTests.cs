using System.Reflection;
using KeyInventory.Application.Reports;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Reports;
using KeyInventory.Web.Pages.Reports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class ReportExportsBoundaryTests
{
    [Fact]
    public void ExportersDoNotAcceptPersistenceTypes()
    {
        foreach (Type exporter in new[] { typeof(ClosedXmlReportExcelExporter), typeof(QuestPdfReportPdfExporter) })
        {
            Assert.DoesNotContain(
                exporter.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter =>
                    parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal)
                    || parameter.ParameterType == typeof(IOperationalReportsPort));
        }

        Assert.Single(
            typeof(LoanVerticalComposition).Assembly.GetTypes(),
            type => type.IsClass && !type.IsAbstract && typeof(IReportExcelExporter).IsAssignableFrom(type));
        Assert.Single(
            typeof(LoanVerticalComposition).Assembly.GetTypes(),
            type => type.IsClass && !type.IsAbstract && typeof(IReportPdfExporter).IsAssignableFrom(type));
    }

    [Fact]
    public void ReportPagesStillConsumeOnlyApplicationUseCase()
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
                    parameter.ParameterType == typeof(IReportExcelExporter)
                    || parameter.ParameterType == typeof(IReportPdfExporter)
                    || parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CompositionRegistersExportAdaptersWithoutSecondStore()
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
        Assert.NotNull(provider.GetService<IReportExcelExporter>());
        Assert.NotNull(provider.GetService<IReportPdfExporter>());
        Assert.IsType<ClosedXmlReportExcelExporter>(provider.GetService<IReportExcelExporter>());
        Assert.IsType<QuestPdfReportPdfExporter>(provider.GetService<IReportPdfExporter>());

        string[] prohibited = typeof(LoanVerticalComposition).Assembly.GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains("Reports2", StringComparison.OrdinalIgnoreCase)
                || name.Contains("DataWarehouse", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(prohibited);
    }
}

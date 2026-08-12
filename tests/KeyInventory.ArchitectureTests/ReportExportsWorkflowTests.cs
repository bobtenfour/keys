using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class ReportExportsWorkflowTests : IAsyncLifetime
{
    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        string connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();
        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, connectionString);
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        if (_services is null)
        {
            return;
        }

        using (IServiceScope scope = _services.CreateScope())
        {
            KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
            await db.Database.EnsureDeletedAsync().ConfigureAwait(true);
        }

        await _services.DisposeAsync().ConfigureAwait(true);
        _services = null;
    }

    [Fact]
    public async Task SevenReportsExportCsvXlsxAndPdfWithParityAndValidStructures()
    {
        using IServiceScope scope = CreateScope();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "rx")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("RX-KEY-1", "01", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("RX-KEY-2", "01", "electronic", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-rx-1",
                "RX-KEY-1",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-rx-2",
                "RX-KEY-2",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(2),
                CancellationToken.None)
            .ConfigureAwait(true);
        await completeReturn.ExecuteAsync("return-rx-2", "loan-rx-2", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset utcNow = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

        IReadOnlyList<CurrentKeyHolderReportRow> holders =
            await reports.ListCurrentKeyHoldersAsync("RX-KEY", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatCurrentKeyHoldersCsv(holders),
            reports.FormatCurrentKeyHoldersXlsx(holders, "Key filter: RX-KEY"),
            reports.FormatCurrentKeyHoldersPdf(holders, "Key filter: RX-KEY"),
            "RX-KEY-1",
            "current-key-holders");

        IReadOnlyList<ActiveLoanReportRow> active =
            await reports.ListActiveLoansReportAsync("RX-KEY", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatActiveLoansCsv(active),
            reports.FormatActiveLoansXlsx(active, "Key filter: RX-KEY"),
            reports.FormatActiveLoansPdf(active, "Key filter: RX-KEY"),
            "RX-KEY-1",
            "active-loans");

        IReadOnlyList<OverdueKeyReportRow> overdue =
            await reports.ListOverdueKeysAsync(utcNow, "RX-KEY", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatOverdueKeysCsv(overdue),
            reports.FormatOverdueKeysXlsx(overdue, "Key filter: RX-KEY"),
            reports.FormatOverdueKeysPdf(overdue, "Key filter: RX-KEY"),
            "RX-KEY-1",
            "overdue-keys");

        KeysByWorkforceMemberReport? memberReport =
            await reports.GetKeysByWorkforceMemberAsync(seeded.MemberCode, CancellationToken.None).ConfigureAwait(true);
        Assert.NotNull(memberReport);
        AssertExportTriplet(
            reports.FormatKeysByWorkforceMemberCsv(memberReport),
            reports.FormatKeysByWorkforceMemberXlsx(memberReport, $"Workforce member: {seeded.MemberCode}"),
            reports.FormatKeysByWorkforceMemberPdf(memberReport, $"Workforce member: {seeded.MemberCode}"),
            "RX-KEY-1",
            $"keys-by-member-{seeded.MemberCode}");

        IReadOnlyList<KeyHistoryReportRow> history =
            await reports.ListKeyHistoryAsync("RX-KEY-2", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatKeyHistoryCsv(history),
            reports.FormatKeyHistoryXlsx(history, "Key filter: RX-KEY-2"),
            reports.FormatKeyHistoryPdf(history, "Key filter: RX-KEY-2"),
            "RX-KEY-2",
            "key-history-RX-KEY-2");

        IReadOnlyList<OutstandingWorkforceKeyReportRow> outstanding =
            await reports.ListOutstandingKeysByWorkforceStatusAsync("Active", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatOutstandingKeysByWorkforceStatusCsv(outstanding),
            reports.FormatOutstandingKeysByWorkforceStatusXlsx(outstanding, "Workforce status: Active"),
            reports.FormatOutstandingKeysByWorkforceStatusPdf(outstanding, "Workforce status: Active"),
            "RX-KEY-1",
            "outstanding-by-workforce-status");

        IReadOnlyList<KeyCatalogReportRow> catalog =
            await reports.ListKeyCatalogReportAsync("RX-KEY", CancellationToken.None).ConfigureAwait(true);
        AssertExportTriplet(
            reports.FormatKeyCatalogCsv(catalog),
            reports.FormatKeyCatalogXlsx(catalog, "Key filter: RX-KEY"),
            reports.FormatKeyCatalogPdf(catalog, "Key filter: RX-KEY"),
            "RX-KEY-2",
            "key-catalog");

        Assert.Equal(
            reports.FormatCurrentKeyHoldersCsv(holders),
            reports.FormatCurrentKeyHoldersCsv(holders));
        Assert.Contains("Ada", reports.FormatCurrentKeyHoldersCsv(holders), StringComparison.Ordinal);
    }

    [Fact]
    public void ZeroRowExportsAreValidAndFormattersDoNotTakeDbContext()
    {
        ClosedXmlReportExcelExporter excel = new();
        QuestPdfReportPdfExporter pdf = new();
        ReportExportTable empty = new(
            "Key Catalog",
            "Key Catalog",
            "Key filter: (none)",
            ["Key", "Type", "Active", "Availability", "Rooms Opened"],
            []);

        byte[] xlsx = excel.Export(empty);
        byte[] pdfBytes = pdf.Export(empty);
        AssertValidXlsx(xlsx);
        AssertValidPdf(pdfBytes);

        using MemoryStream stream = new(xlsx);
        using XLWorkbook workbook = new(stream);
        Assert.Contains("Key Catalog", workbook.Worksheet(1).Cell(1, 1).GetString(), StringComparison.Ordinal);

        Assert.DoesNotContain(
            typeof(ClosedXmlReportExcelExporter).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(QuestPdfReportPdfExporter).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(LoanVerticalComposition).Assembly.GetTypes()
                .Where(type => typeof(IReportExcelExporter).IsAssignableFrom(type) && type.IsClass),
            type => type.GetConstructors().SelectMany(ctor => ctor.GetParameters())
                .Any(parameter => parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal)));
    }

    [Fact]
    public void ExportResultFactoryUsesCorrectMimeTypesAndFilenames()
    {
        FileContentResult csv = Assert.IsType<FileContentResult>(
            ReportExportResultFactory.Create(
                "csv",
                "sample-report",
                () => "Key,Type\nA,B\n",
                () => [0x50, 0x4B],
                () => Encoding.ASCII.GetBytes("%PDF-1.4")));
        Assert.Equal("text/csv; charset=utf-8", csv.ContentType);
        Assert.Equal("sample-report.csv", csv.FileDownloadName);

        FileContentResult xlsx = Assert.IsType<FileContentResult>(
            ReportExportResultFactory.Create(
                "xlsx",
                "sample-report",
                () => "x",
                () => [0x50, 0x4B, 0x03, 0x04],
                () => Encoding.ASCII.GetBytes("%PDF-1.4")));
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            xlsx.ContentType);
        Assert.Equal("sample-report.xlsx", xlsx.FileDownloadName);

        FileContentResult pdf = Assert.IsType<FileContentResult>(
            ReportExportResultFactory.Create(
                "pdf",
                "sample-report",
                () => "x",
                () => [0x50, 0x4B],
                () => Encoding.ASCII.GetBytes("%PDF-1.4")));
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.Equal("sample-report.pdf", pdf.FileDownloadName);

        Assert.IsType<BadRequestObjectResult>(
            ReportExportResultFactory.Create("xml", "sample", () => "", () => [], () => []));
    }

    private static void AssertExportTriplet(
        string csv,
        byte[] xlsx,
        byte[] pdf,
        string expectedToken,
        string fileStem)
    {
        Assert.Contains(expectedToken, csv, StringComparison.Ordinal);
        AssertValidXlsx(xlsx);
        AssertValidPdf(pdf);

        using MemoryStream stream = new(xlsx);
        using XLWorkbook workbook = new(stream);
        string sheetText = string.Join(
            ' ',
            workbook.Worksheet(1).RangeUsed()?.CellsUsed().Select(cell => cell.GetString()) ?? []);
        Assert.Contains(expectedToken, sheetText, StringComparison.Ordinal);

        string pdfText = Encoding.Latin1.GetString(pdf);
        Assert.Contains("%PDF", pdfText, StringComparison.Ordinal);

        FileContentResult csvResult = ReportExportResultFactory.CreateCsv(fileStem, csv);
        Assert.EndsWith(".csv", csvResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        FileContentResult xlsxResult = ReportExportResultFactory.CreateXlsx(fileStem, xlsx);
        Assert.EndsWith(".xlsx", xlsxResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        FileContentResult pdfResult = ReportExportResultFactory.CreatePdf(fileStem, pdf);
        Assert.EndsWith(".pdf", pdfResult.FileDownloadName, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertValidXlsx(byte[] bytes)
    {
        Assert.True(bytes.Length > 4);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
        using MemoryStream stream = new(bytes);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName.Contains("xl/", StringComparison.Ordinal));
    }

    private static void AssertValidPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 5);
        string header = Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}

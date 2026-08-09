using KeyInventory.Application.Catalog;

namespace KeyInventory.Application.Reports;

public sealed class OperationalReportsUseCase : IOperationalReportsUseCase
{
    private readonly IOperationalReportsPort _reports;
    private readonly IReportExcelExporter _excelExporter;
    private readonly IReportPdfExporter _pdfExporter;

    public OperationalReportsUseCase(
        IOperationalReportsPort reports,
        IReportExcelExporter excelExporter,
        IReportPdfExporter pdfExporter)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _excelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));
        _pdfExporter = pdfExporter ?? throw new ArgumentNullException(nameof(pdfExporter));
    }

    public Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListCurrentKeyHoldersAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatCurrentKeyHoldersCsv(IReadOnlyList<CurrentKeyHolderReportRow> rows)
    {
        return FormatCsv(BuildCurrentKeyHoldersTable(rows, null));
    }

    public byte[] FormatCurrentKeyHoldersXlsx(IReadOnlyList<CurrentKeyHolderReportRow> rows, string? filterContext)
    {
        return _excelExporter.Export(BuildCurrentKeyHoldersTable(rows, filterContext));
    }

    public byte[] FormatCurrentKeyHoldersPdf(IReadOnlyList<CurrentKeyHolderReportRow> rows, string? filterContext)
    {
        return _pdfExporter.Export(BuildCurrentKeyHoldersTable(rows, filterContext));
    }

    public Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListActiveLoansReportAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatActiveLoansCsv(IReadOnlyList<ActiveLoanReportRow> rows)
    {
        return FormatCsv(BuildActiveLoansTable(rows, null));
    }

    public byte[] FormatActiveLoansXlsx(IReadOnlyList<ActiveLoanReportRow> rows, string? filterContext)
    {
        return _excelExporter.Export(BuildActiveLoansTable(rows, filterContext));
    }

    public byte[] FormatActiveLoansPdf(IReadOnlyList<ActiveLoanReportRow> rows, string? filterContext)
    {
        return _pdfExporter.Export(BuildActiveLoansTable(rows, filterContext));
    }

    public Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Overdue derivation requires a UTC timestamp.");
        }

        return _reports.ListOverdueKeysAsync(utcNow, NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatOverdueKeysCsv(IReadOnlyList<OverdueKeyReportRow> rows)
    {
        return FormatCsv(BuildOverdueKeysTable(rows, null));
    }

    public byte[] FormatOverdueKeysXlsx(IReadOnlyList<OverdueKeyReportRow> rows, string? filterContext)
    {
        return _excelExporter.Export(BuildOverdueKeysTable(rows, filterContext));
    }

    public byte[] FormatOverdueKeysPdf(IReadOnlyList<OverdueKeyReportRow> rows, string? filterContext)
    {
        return _pdfExporter.Export(BuildOverdueKeysTable(rows, filterContext));
    }

    public Task<KeysByWorkforceMemberReport?> GetKeysByWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workforceMemberCode))
        {
            throw new ArgumentException("Workforce member code is required.", nameof(workforceMemberCode));
        }

        return _reports.GetKeysByWorkforceMemberAsync(workforceMemberCode.Trim(), cancellationToken);
    }

    public string FormatKeysByWorkforceMemberCsv(KeysByWorkforceMemberReport report)
    {
        return FormatCsv(BuildKeysByWorkforceMemberTable(report, null));
    }

    public byte[] FormatKeysByWorkforceMemberXlsx(KeysByWorkforceMemberReport report, string? filterContext)
    {
        return _excelExporter.Export(BuildKeysByWorkforceMemberTable(report, filterContext));
    }

    public byte[] FormatKeysByWorkforceMemberPdf(KeysByWorkforceMemberReport report, string? filterContext)
    {
        return _pdfExporter.Export(BuildKeysByWorkforceMemberTable(report, filterContext));
    }

    public Task<IReadOnlyList<KeyHistoryReportRow>> ListKeyHistoryAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(catalogKeyCode))
        {
            throw new ArgumentException("Catalog key code is required.", nameof(catalogKeyCode));
        }

        return _reports.ListKeyHistoryAsync(catalogKeyCode.Trim(), cancellationToken);
    }

    public string FormatKeyHistoryCsv(IReadOnlyList<KeyHistoryReportRow> rows)
    {
        return FormatCsv(BuildKeyHistoryTable(rows, null));
    }

    public byte[] FormatKeyHistoryXlsx(IReadOnlyList<KeyHistoryReportRow> rows, string? filterContext)
    {
        return _excelExporter.Export(BuildKeyHistoryTable(rows, filterContext));
    }

    public byte[] FormatKeyHistoryPdf(IReadOnlyList<KeyHistoryReportRow> rows, string? filterContext)
    {
        return _pdfExporter.Export(BuildKeyHistoryTable(rows, filterContext));
    }

    public Task<IReadOnlyList<OutstandingWorkforceKeyReportRow>> ListOutstandingKeysByWorkforceStatusAsync(
        string? workforceStatusFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListOutstandingKeysByWorkforceStatusAsync(
            NormalizeOptional(workforceStatusFilter),
            cancellationToken);
    }

    public string FormatOutstandingKeysByWorkforceStatusCsv(IReadOnlyList<OutstandingWorkforceKeyReportRow> rows)
    {
        return FormatCsv(BuildOutstandingTable(rows, null));
    }

    public byte[] FormatOutstandingKeysByWorkforceStatusXlsx(
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows,
        string? filterContext)
    {
        return _excelExporter.Export(BuildOutstandingTable(rows, filterContext));
    }

    public byte[] FormatOutstandingKeysByWorkforceStatusPdf(
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows,
        string? filterContext)
    {
        return _pdfExporter.Export(BuildOutstandingTable(rows, filterContext));
    }

    public Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListKeyCatalogReportAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatKeyCatalogCsv(IReadOnlyList<KeyCatalogReportRow> rows)
    {
        return FormatCsv(BuildKeyCatalogTable(rows, null));
    }

    public byte[] FormatKeyCatalogXlsx(IReadOnlyList<KeyCatalogReportRow> rows, string? filterContext)
    {
        return _excelExporter.Export(BuildKeyCatalogTable(rows, filterContext));
    }

    public byte[] FormatKeyCatalogPdf(IReadOnlyList<KeyCatalogReportRow> rows, string? filterContext)
    {
        return _pdfExporter.Export(BuildKeyCatalogTable(rows, filterContext));
    }

    public Task<IReadOnlyList<WorkforceMemberReportOption>> ListWorkforceMemberOptionsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        return _reports.ListWorkforceMemberOptionsAsync(NormalizeOptional(search), cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListCatalogKeyCodesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        return _reports.ListCatalogKeyCodesAsync(NormalizeOptional(search), cancellationToken);
    }

    private static string FormatCsv(ReportExportTable table)
    {
        return ReportCsvFormatter.Build(
            table.Headers,
            table.Rows.Select(row => (IReadOnlyList<string>)row.Select(cell => cell.Text).ToArray()));
    }

    private static ReportExportTable BuildCurrentKeyHoldersTable(
        IReadOnlyList<CurrentKeyHolderReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Current Key Holders",
            "Current Key Holders",
            filterContext,
            [
                "Key",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Workforce Member",
                "Department",
                "Responsible Manager",
                "Issued At (UTC)",
                "Due At (UTC)",
                "Status"
            ],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.HolderFirstName),
                ReportExportCell.FromText(row.HolderLastName),
                ReportExportCell.FromText(row.HolderUin),
                ReportExportCell.FromText(row.WorkforceMemberCode),
                ReportExportCell.FromText(row.DepartmentCode),
                ReportExportCell.FromText(row.ResponsibleManagerWorkforceMemberCode),
                ReportExportCell.DateTimeUtcValue(row.IssuedAtUtc),
                ReportExportCell.DateTimeUtcValue(row.DueAtUtc),
                ReportExportCell.FromText(row.Status)
            ]).ToArray());
    }

    private static ReportExportTable BuildActiveLoansTable(
        IReadOnlyList<ActiveLoanReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Active Loans",
            "Active Loans",
            filterContext,
            [
                "Key",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Workforce Member",
                "Department",
                "Issued At (UTC)",
                "Due At (UTC)",
                "Status"
            ],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.HolderFirstName),
                ReportExportCell.FromText(row.HolderLastName),
                ReportExportCell.FromText(row.HolderUin),
                ReportExportCell.FromText(row.WorkforceMemberCode),
                ReportExportCell.FromText(row.DepartmentCode),
                ReportExportCell.DateTimeUtcValue(row.IssuedAtUtc),
                ReportExportCell.DateTimeUtcValue(row.DueAtUtc),
                ReportExportCell.FromText(row.Status)
            ]).ToArray());
    }

    private static ReportExportTable BuildOverdueKeysTable(
        IReadOnlyList<OverdueKeyReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Overdue Keys",
            "Overdue Keys",
            filterContext,
            [
                "Key",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Workforce Member",
                "Responsible Manager",
                "Department",
                "Issued At (UTC)",
                "Due At (UTC)",
                "Days Overdue",
                "Status"
            ],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.HolderFirstName),
                ReportExportCell.FromText(row.HolderLastName),
                ReportExportCell.FromText(row.HolderUin),
                ReportExportCell.FromText(row.WorkforceMemberCode),
                ReportExportCell.FromText(row.ResponsibleManagerWorkforceMemberCode),
                ReportExportCell.FromText(row.DepartmentCode),
                ReportExportCell.DateTimeUtcValue(row.IssuedAtUtc),
                ReportExportCell.DateTimeUtcValue(row.DueAtUtc),
                ReportExportCell.WholeNumber(row.DaysOverdue),
                ReportExportCell.FromText(row.Status)
            ]).ToArray());
    }

    private static ReportExportTable BuildKeysByWorkforceMemberTable(
        KeysByWorkforceMemberReport report,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(report);
        List<IReadOnlyList<ReportExportCell>> rows = [];
        foreach (MemberIssuedKeyReportRow issued in report.IssuedKeys)
        {
            rows.Add(
            [
                ReportExportCell.FromText(report.WorkforceMemberCode),
                ReportExportCell.FromText("Issued"),
                ReportExportCell.FromText(issued.CatalogKeyCode),
                ReportExportCell.FromText(issued.HolderFirstName),
                ReportExportCell.FromText(issued.HolderLastName),
                ReportExportCell.FromText(issued.HolderUin),
                ReportExportCell.DateTimeUtcValue(issued.IssuedAtUtc),
                ReportExportCell.DateTimeUtcValue(issued.DueAtUtc),
                ReportExportCell.FromText(string.Empty),
                ReportExportCell.FromText(issued.Status)
            ]);
        }

        foreach (MemberReturnedKeyReportRow returned in report.ReturnedKeys)
        {
            rows.Add(
            [
                ReportExportCell.FromText(report.WorkforceMemberCode),
                ReportExportCell.FromText("Returned"),
                ReportExportCell.FromText(returned.CatalogKeyCode),
                ReportExportCell.FromText(returned.HolderFirstName),
                ReportExportCell.FromText(returned.HolderLastName),
                ReportExportCell.FromText(returned.HolderUin),
                ReportExportCell.DateTimeUtcValue(returned.IssuedAtUtc),
                ReportExportCell.FromText(string.Empty),
                ReportExportCell.DateTimeUtcValue(returned.ReturnedAtUtc),
                ReportExportCell.FromText(returned.Status)
            ]);
        }

        return new ReportExportTable(
            "Keys by Workforce Member",
            "Keys by Member",
            filterContext ?? $"Workforce member: {report.WorkforceMemberCode}",
            [
                "Workforce Member",
                "Row Kind",
                "Key",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Issued At (UTC)",
                "Due At (UTC)",
                "Returned At (UTC)",
                "Status"
            ],
            rows);
    }

    private static ReportExportTable BuildKeyHistoryTable(
        IReadOnlyList<KeyHistoryReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Key History",
            "Key History",
            filterContext,
            [
                "Loan",
                "Key",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Issued At (UTC)",
                "Due At (UTC)",
                "Returned At (UTC)",
                "Status"
            ],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.LoanCode),
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.HolderFirstName),
                ReportExportCell.FromText(row.HolderLastName),
                ReportExportCell.FromText(row.HolderUin),
                ReportExportCell.DateTimeUtcValue(row.IssuedAtUtc),
                ReportExportCell.DateTimeUtcValue(row.DueAtUtc),
                ReportExportCell.OptionalDateTimeUtc(row.ReturnedAtUtc),
                ReportExportCell.FromText(row.Status)
            ]).ToArray());
    }

    private static ReportExportTable BuildOutstandingTable(
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Outstanding Keys by Workforce Status",
            "Outstanding Keys",
            filterContext,
            [
                "Workforce Member",
                "Workforce Status",
                "Holder First Name",
                "Holder Last Name",
                "UIN",
                "Department",
                "Responsible Manager",
                "Key",
                "Loan",
                "Due At (UTC)"
            ],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.WorkforceMemberCode),
                ReportExportCell.FromText(row.WorkforceMemberStatus),
                ReportExportCell.FromText(row.HolderFirstName),
                ReportExportCell.FromText(row.HolderLastName),
                ReportExportCell.FromText(row.HolderUin),
                ReportExportCell.FromText(row.DepartmentCode),
                ReportExportCell.FromText(row.ResponsibleManagerWorkforceMemberCode),
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.LoanCode),
                ReportExportCell.DateTimeUtcValue(row.DueAtUtc)
            ]).ToArray());
    }

    private static ReportExportTable BuildKeyCatalogTable(
        IReadOnlyList<KeyCatalogReportRow> rows,
        string? filterContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new ReportExportTable(
            "Key Catalog",
            "Key Catalog",
            filterContext,
            ["Key", "Type", "Active", "Availability", "Rooms Opened"],
            rows.Select(row => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.FromText(row.CatalogKeyCode),
                ReportExportCell.FromText(row.TypeCode),
                ReportExportCell.FromText(row.IsActive ? "Yes" : "No"),
                ReportExportCell.FromText(row.AvailabilityStatus),
                ReportExportCell.FromText(KeyOpenedRoomDisplayFormatter.Format(row.OpenedRooms))
            ]).ToArray());
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

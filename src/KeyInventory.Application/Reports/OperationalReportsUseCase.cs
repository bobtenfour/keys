namespace KeyInventory.Application.Reports;

public sealed class OperationalReportsUseCase : IOperationalReportsUseCase
{
    private readonly IOperationalReportsPort _reports;

    public OperationalReportsUseCase(IOperationalReportsPort reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListCurrentKeyHoldersAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatCurrentKeyHoldersCsv(IReadOnlyList<CurrentKeyHolderReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
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
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.CatalogKeyCode,
                row.HolderFirstName,
                row.HolderLastName,
                row.HolderUin,
                row.WorkforceMemberCode ?? string.Empty,
                row.DepartmentCode ?? string.Empty,
                row.ResponsibleManagerWorkforceMemberCode ?? string.Empty,
                ReportCsvFormatter.FormatTimestamp(row.IssuedAtUtc),
                ReportCsvFormatter.FormatTimestamp(row.DueAtUtc),
                row.Status
            ]));
    }

    public Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListActiveLoansReportAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatActiveLoansCsv(IReadOnlyList<ActiveLoanReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
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
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.CatalogKeyCode,
                row.HolderFirstName,
                row.HolderLastName,
                row.HolderUin,
                row.WorkforceMemberCode ?? string.Empty,
                row.DepartmentCode ?? string.Empty,
                ReportCsvFormatter.FormatTimestamp(row.IssuedAtUtc),
                ReportCsvFormatter.FormatTimestamp(row.DueAtUtc),
                row.Status
            ]));
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
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
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
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.CatalogKeyCode,
                row.HolderFirstName,
                row.HolderLastName,
                row.HolderUin,
                row.WorkforceMemberCode ?? string.Empty,
                row.ResponsibleManagerWorkforceMemberCode ?? string.Empty,
                row.DepartmentCode ?? string.Empty,
                ReportCsvFormatter.FormatTimestamp(row.IssuedAtUtc),
                ReportCsvFormatter.FormatTimestamp(row.DueAtUtc),
                row.DaysOverdue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Status
            ]));
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
        ArgumentNullException.ThrowIfNull(report);
        List<IReadOnlyList<string>> rows = [];
        foreach (MemberIssuedKeyReportRow issued in report.IssuedKeys)
        {
            rows.Add(
            [
                report.WorkforceMemberCode,
                "Issued",
                issued.CatalogKeyCode,
                issued.HolderFirstName,
                issued.HolderLastName,
                issued.HolderUin,
                ReportCsvFormatter.FormatTimestamp(issued.IssuedAtUtc),
                ReportCsvFormatter.FormatTimestamp(issued.DueAtUtc),
                string.Empty,
                issued.Status
            ]);
        }

        foreach (MemberReturnedKeyReportRow returned in report.ReturnedKeys)
        {
            rows.Add(
            [
                report.WorkforceMemberCode,
                "Returned",
                returned.CatalogKeyCode,
                returned.HolderFirstName,
                returned.HolderLastName,
                returned.HolderUin,
                ReportCsvFormatter.FormatTimestamp(returned.IssuedAtUtc),
                string.Empty,
                ReportCsvFormatter.FormatTimestamp(returned.ReturnedAtUtc),
                returned.Status
            ]);
        }

        return ReportCsvFormatter.Build(
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
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
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
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.LoanCode,
                row.CatalogKeyCode,
                row.HolderFirstName,
                row.HolderLastName,
                row.HolderUin,
                ReportCsvFormatter.FormatTimestamp(row.IssuedAtUtc),
                ReportCsvFormatter.FormatTimestamp(row.DueAtUtc),
                row.ReturnedAtUtc is null ? string.Empty : ReportCsvFormatter.FormatTimestamp(row.ReturnedAtUtc.Value),
                row.Status
            ]));
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
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
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
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.WorkforceMemberCode,
                row.WorkforceMemberStatus,
                row.HolderFirstName,
                row.HolderLastName,
                row.HolderUin,
                row.DepartmentCode,
                row.ResponsibleManagerWorkforceMemberCode,
                row.CatalogKeyCode,
                row.LoanCode,
                ReportCsvFormatter.FormatTimestamp(row.DueAtUtc)
            ]));
    }

    public Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        return _reports.ListKeyCatalogReportAsync(NormalizeOptional(catalogKeyCodeFilter), cancellationToken);
    }

    public string FormatKeyCatalogCsv(IReadOnlyList<KeyCatalogReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return ReportCsvFormatter.Build(
            ["Key", "Type", "Active", "Availability"],
            rows.Select(row => (IReadOnlyList<string>)
            [
                row.CatalogKeyCode,
                row.TypeCode,
                row.IsActive ? "Yes" : "No",
                row.AvailabilityStatus
            ]));
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

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

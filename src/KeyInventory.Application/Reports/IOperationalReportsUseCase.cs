namespace KeyInventory.Application.Reports;

public interface IOperationalReportsUseCase
{
    Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatCurrentKeyHoldersCsv(IReadOnlyList<CurrentKeyHolderReportRow> rows);

    Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatActiveLoansCsv(IReadOnlyList<ActiveLoanReportRow> rows);

    Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatOverdueKeysCsv(IReadOnlyList<OverdueKeyReportRow> rows);

    Task<KeysByWorkforceMemberReport?> GetKeysByWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    string FormatKeysByWorkforceMemberCsv(KeysByWorkforceMemberReport report);

    Task<IReadOnlyList<KeyHistoryReportRow>> ListKeyHistoryAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);

    string FormatKeyHistoryCsv(IReadOnlyList<KeyHistoryReportRow> rows);

    Task<IReadOnlyList<OutstandingWorkforceKeyReportRow>> ListOutstandingKeysByWorkforceStatusAsync(
        string? workforceStatusFilter,
        CancellationToken cancellationToken);

    string FormatOutstandingKeysByWorkforceStatusCsv(IReadOnlyList<OutstandingWorkforceKeyReportRow> rows);

    Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatKeyCatalogCsv(IReadOnlyList<KeyCatalogReportRow> rows);

    Task<IReadOnlyList<WorkforceMemberReportOption>> ListWorkforceMemberOptionsAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListCatalogKeyCodesAsync(
        string? search,
        CancellationToken cancellationToken);
}

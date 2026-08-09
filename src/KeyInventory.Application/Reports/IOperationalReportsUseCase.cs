namespace KeyInventory.Application.Reports;

public interface IOperationalReportsUseCase
{
    Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatCurrentKeyHoldersCsv(IReadOnlyList<CurrentKeyHolderReportRow> rows);

    byte[] FormatCurrentKeyHoldersXlsx(IReadOnlyList<CurrentKeyHolderReportRow> rows, string? filterContext);

    byte[] FormatCurrentKeyHoldersPdf(IReadOnlyList<CurrentKeyHolderReportRow> rows, string? filterContext);

    Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatActiveLoansCsv(IReadOnlyList<ActiveLoanReportRow> rows);

    byte[] FormatActiveLoansXlsx(IReadOnlyList<ActiveLoanReportRow> rows, string? filterContext);

    byte[] FormatActiveLoansPdf(IReadOnlyList<ActiveLoanReportRow> rows, string? filterContext);

    Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatOverdueKeysCsv(IReadOnlyList<OverdueKeyReportRow> rows);

    byte[] FormatOverdueKeysXlsx(IReadOnlyList<OverdueKeyReportRow> rows, string? filterContext);

    byte[] FormatOverdueKeysPdf(IReadOnlyList<OverdueKeyReportRow> rows, string? filterContext);

    Task<KeysByWorkforceMemberReport?> GetKeysByWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    string FormatKeysByWorkforceMemberCsv(KeysByWorkforceMemberReport report);

    byte[] FormatKeysByWorkforceMemberXlsx(KeysByWorkforceMemberReport report, string? filterContext);

    byte[] FormatKeysByWorkforceMemberPdf(KeysByWorkforceMemberReport report, string? filterContext);

    Task<IReadOnlyList<KeyHistoryReportRow>> ListKeyHistoryAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);

    string FormatKeyHistoryCsv(IReadOnlyList<KeyHistoryReportRow> rows);

    byte[] FormatKeyHistoryXlsx(IReadOnlyList<KeyHistoryReportRow> rows, string? filterContext);

    byte[] FormatKeyHistoryPdf(IReadOnlyList<KeyHistoryReportRow> rows, string? filterContext);

    Task<IReadOnlyList<OutstandingWorkforceKeyReportRow>> ListOutstandingKeysByWorkforceStatusAsync(
        string? workforceStatusFilter,
        CancellationToken cancellationToken);

    string FormatOutstandingKeysByWorkforceStatusCsv(IReadOnlyList<OutstandingWorkforceKeyReportRow> rows);

    byte[] FormatOutstandingKeysByWorkforceStatusXlsx(
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows,
        string? filterContext);

    byte[] FormatOutstandingKeysByWorkforceStatusPdf(
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows,
        string? filterContext);

    Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    string FormatKeyCatalogCsv(IReadOnlyList<KeyCatalogReportRow> rows);

    byte[] FormatKeyCatalogXlsx(IReadOnlyList<KeyCatalogReportRow> rows, string? filterContext);

    byte[] FormatKeyCatalogPdf(IReadOnlyList<KeyCatalogReportRow> rows, string? filterContext);

    Task<IReadOnlyList<WorkforceMemberReportOption>> ListWorkforceMemberOptionsAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListCatalogKeyCodesAsync(
        string? search,
        CancellationToken cancellationToken);
}

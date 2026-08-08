namespace KeyInventory.Application.Reports;

public interface IOperationalReportsPort
{
    Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    Task<KeysByWorkforceMemberReport?> GetKeysByWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyHistoryReportRow>> ListKeyHistoryAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutstandingWorkforceKeyReportRow>> ListOutstandingKeysByWorkforceStatusAsync(
        string? workforceStatusFilter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberReportOption>> ListWorkforceMemberOptionsAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListCatalogKeyCodesAsync(
        string? search,
        CancellationToken cancellationToken);
}

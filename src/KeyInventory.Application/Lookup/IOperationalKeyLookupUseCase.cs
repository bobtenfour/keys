namespace KeyInventory.Application.Lookup;

/// <summary>
/// Application-owned operational key lookup/read authority consumed by Operations → Find Key,
/// operational identity surfaces, and Workforce Member issued-key paths.
/// Header global search is owned by <see cref="IGlobalOperatorSearchUseCase"/>.
/// </summary>
public interface IOperationalKeyLookupUseCase
{
    Task<IReadOnlyList<KeyLookupResult>> SearchKeysAsync(string query, CancellationToken cancellationToken);

    Task<IReadOnlyList<IssuedKeyForMemberItem>> ListIssuedKeysForWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalLoanDisplay>> ListOpenLoansWithHoldersAsync(CancellationToken cancellationToken);

    const int DefaultOpenLoanSearchMaxResults = 25;

    Task<IReadOnlyList<OperationalLoanDisplay>> SearchOpenLoansWithHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    Task<OperationalLoanDisplay?> FindOpenLoanByLoanCodeAsync(string loanCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberIdentityDisplay>> ListActiveWorkforceMembersWithIdentityAsync(
        CancellationToken cancellationToken);
}

namespace KeyInventory.Application.Lookup;

/// <summary>
/// Single Application read authority for operational key/holder lookup over existing SQL Server data.
/// </summary>
public interface IOperationalKeyLookupPort
{
    Task<IReadOnlyList<KeyLookupResult>> SearchKeysAsync(string query, CancellationToken cancellationToken);

    Task<IReadOnlyList<IssuedKeyForMemberItem>> ListIssuedKeysForWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalLoanDisplay>> ListOpenLoansWithHoldersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Bounded search of open loans by KEY #, MEDECO, holder name, or UIN.
    /// </summary>
    Task<IReadOnlyList<OperationalLoanDisplay>> SearchOpenLoansWithHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    Task<OperationalLoanDisplay?> FindOpenLoanByLoanCodeAsync(string loanCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberIdentityDisplay>> ListActiveWorkforceMembersWithIdentityAsync(
        CancellationToken cancellationToken);
}

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

    Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberIdentityDisplay>> ListActiveWorkforceMembersWithIdentityAsync(
        CancellationToken cancellationToken);
}

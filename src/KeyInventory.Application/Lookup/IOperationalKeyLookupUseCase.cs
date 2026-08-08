namespace KeyInventory.Application.Lookup;

/// <summary>
/// Application-owned operational lookup/read authority consumed by Find Key, header search,
/// operational identity surfaces, and Workforce Member issued-key paths.
/// </summary>
public interface IOperationalKeyLookupUseCase
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

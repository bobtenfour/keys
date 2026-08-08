namespace KeyInventory.Application.Lookup;

public sealed class OperationalKeyLookupUseCase : IOperationalKeyLookupUseCase
{
    private readonly IOperationalKeyLookupPort _lookup;

    public OperationalKeyLookupUseCase(IOperationalKeyLookupPort lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public Task<IReadOnlyList<KeyLookupResult>> SearchKeysAsync(string query, CancellationToken cancellationToken)
    {
        string normalized = (query ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<KeyLookupResult>>([]);
        }

        return _lookup.SearchKeysAsync(normalized, cancellationToken);
    }

    public Task<IReadOnlyList<IssuedKeyForMemberItem>> ListIssuedKeysForWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workforceMemberCode))
        {
            throw new ArgumentException("Workforce member code is required.", nameof(workforceMemberCode));
        }

        return _lookup.ListIssuedKeysForWorkforceMemberAsync(workforceMemberCode.Trim(), cancellationToken);
    }

    public Task<IReadOnlyList<OperationalLoanDisplay>> ListOpenLoansWithHoldersAsync(CancellationToken cancellationToken)
    {
        return _lookup.ListOpenLoansWithHoldersAsync(cancellationToken);
    }

    public Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(
        CancellationToken cancellationToken)
    {
        return _lookup.ListReturnedLoansWithHoldersAsync(cancellationToken);
    }

    public Task<IReadOnlyList<WorkforceMemberIdentityDisplay>> ListActiveWorkforceMembersWithIdentityAsync(
        CancellationToken cancellationToken)
    {
        return _lookup.ListActiveWorkforceMembersWithIdentityAsync(cancellationToken);
    }
}

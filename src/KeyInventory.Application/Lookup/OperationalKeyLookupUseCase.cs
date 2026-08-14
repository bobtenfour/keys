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

    public Task<IReadOnlyList<OperationalLoanDisplay>> SearchOpenLoansWithHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        string normalized = (searchText ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<OperationalLoanDisplay>>([]);
        }

        int bound = maxResults < 1
            ? IOperationalKeyLookupUseCase.DefaultOpenLoanSearchMaxResults
            : Math.Min(maxResults, IOperationalKeyLookupUseCase.DefaultOpenLoanSearchMaxResults);
        return _lookup.SearchOpenLoansWithHoldersAsync(normalized, bound, cancellationToken);
    }

    public Task<OperationalLoanDisplay?> FindOpenLoanByLoanCodeAsync(
        string loanCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loanCode))
        {
            return Task.FromResult<OperationalLoanDisplay?>(null);
        }

        return _lookup.FindOpenLoanByLoanCodeAsync(loanCode.Trim(), cancellationToken);
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

namespace KeyInventory.Application.Lookup;

/// <summary>
/// Bounded browse + search of open loans for the Receive combobox.
/// Empty query returns the first <see cref="DefaultMaxResults"/> open loans
/// ordered by LoanCode. Non-empty queries match against KEY # / MEDECO / holder name / UIN.
/// </summary>
public interface ISearchOpenCustodyUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<OperationalLoanDisplay>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class SearchOpenCustodyUseCase : ISearchOpenCustodyUseCase
{
    private readonly IOperationalKeyLookupPort _lookup;

    public SearchOpenCustodyUseCase(IOperationalKeyLookupPort lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public async Task<IReadOnlyList<OperationalLoanDisplay>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        int bound = maxResults < 1
            ? ISearchOpenCustodyUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchOpenCustodyUseCase.DefaultMaxResults);

        string term = (searchText ?? string.Empty).Trim();
        if (term.Length == 0)
        {
            IReadOnlyList<OperationalLoanDisplay> all = await _lookup
                .ListOpenLoansWithHoldersAsync(cancellationToken)
                .ConfigureAwait(false);
            return all.Take(bound).ToArray();
        }

        return await _lookup
            .SearchOpenLoansWithHoldersAsync(term, bound, cancellationToken)
            .ConfigureAwait(false);
    }
}

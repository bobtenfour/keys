using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Workflow;

public interface ILoanPersistencePort
{
    Task<bool> LoanExistsAsync(string loanCode, CancellationToken cancellationToken);

    Task AddLoanAsync(Loan loan, CancellationToken cancellationToken);

    Task<Loan?> FindOpenLoanAsync(string loanCode, CancellationToken cancellationToken);

    Task AddReturnAsync(Return completedReturn, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanListItem>> ListOpenLoansAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanListItem>> ListReturnedLoansAsync(CancellationToken cancellationToken);

    Task<bool> HasOpenLoanForKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task<int> CountLoansForKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task<int> CountLoansForPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task<int> CountLoansJustifiedByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<int> CountLoansJustifiedByRoomAsync(string roomCode, CancellationToken cancellationToken);
}

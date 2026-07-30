using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Loans;

public interface IReturnLookupPort
{
    ValueTask<Return?> FindByReturnCodeAsync(
        string returnCode,
        CancellationToken cancellationToken);
}

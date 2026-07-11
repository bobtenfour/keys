using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public interface ISecurityPrincipalRepository
{
    ValueTask<SecurityPrincipal?> FindByPrincipalNameAsync(
        string principalName,
        CancellationToken cancellationToken);

    ValueTask AddAsync(
        SecurityPrincipal principal,
        CancellationToken cancellationToken);
}

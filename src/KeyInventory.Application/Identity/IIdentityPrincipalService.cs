using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public interface IIdentityPrincipalService
{
    ValueTask<SecurityPrincipal?> FindByPrincipalNameAsync(
        string principalName,
        CancellationToken cancellationToken);

    ValueTask<SecurityPrincipal> CreateAsync(
        string principalName,
        SecurityPrincipalType principalType,
        string? partyReference,
        CancellationToken cancellationToken);
}

using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public interface IRoleAssignmentService
{
    ValueTask<PrincipalRoleAssignment> AssignRoleAsync(
        string principalName,
        string organizationCode,
        string roleCode,
        AuthorizationScopeType scopeType,
        string scopeCode,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        CancellationToken cancellationToken);
}

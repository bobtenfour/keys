using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public sealed record AuthorizationRequest(
    string PrincipalName,
    string PermissionCode,
    AuthorizationScopeType ScopeType,
    string ScopeCode,
    DateTimeOffset RequestedAtUtc);

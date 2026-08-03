namespace KeyInventory.Domain.Identity;

public sealed class PrincipalRoleAssignment
{
    public PrincipalRoleAssignment(
        SecurityPrincipal principal,
        Role role,
        AuthorizationScopeType scopeType,
        string scopeCode,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc)
    {
        Principal = principal ?? throw new ArgumentNullException(nameof(principal));
        Role = role ?? throw new ArgumentNullException(nameof(role));
        ScopeType = scopeType;
        ScopeCode = IdentityText.Require(scopeCode, nameof(scopeCode));
        EffectiveFromUtc = UtcTimestamp.Require(effectiveFromUtc, nameof(effectiveFromUtc));
        EffectiveToUtc = effectiveToUtc is null
            ? null
            : UtcTimestamp.Require(effectiveToUtc.Value, nameof(effectiveToUtc));

        if (scopeType == AuthorizationScopeType.None)
        {
            throw new ArgumentException("AuthorizationScopeType is required.", nameof(scopeType));
        }

        if (EffectiveToUtc is not null && EffectiveToUtc <= EffectiveFromUtc)
        {
            throw new ArgumentException("EffectiveToUtc must be later than EffectiveFromUtc.", nameof(effectiveToUtc));
        }
    }

    public SecurityPrincipal Principal { get; }

    public Role Role { get; }

    public AuthorizationScopeType ScopeType { get; }

    public string ScopeCode { get; }

    public DateTimeOffset EffectiveFromUtc { get; }

    public DateTimeOffset? EffectiveToUtc { get; }

    public bool IsActiveAt(DateTimeOffset instantUtc)
    {
        return EffectiveFromUtc <= instantUtc && (EffectiveToUtc is null || instantUtc < EffectiveToUtc);
    }

    public bool MatchesActiveScope(SecurityPrincipal principal, Role role, AuthorizationScopeType scopeType, string scopeCode)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(role);

        return Principal.PrincipalName == principal.PrincipalName
            && Role.RoleCode == role.RoleCode
            && Role.OrganizationCode == role.OrganizationCode
            && ScopeType == scopeType
            && string.Equals(ScopeCode, scopeCode, StringComparison.Ordinal);
    }
}

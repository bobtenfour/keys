namespace KeyInventory.Domain.Identity;

public sealed class SecurityPrincipal
{
    private readonly List<PrincipalRoleAssignment> _roleAssignments = [];

    public SecurityPrincipal(string principalName, SecurityPrincipalType principalType, string? partyReference)
    {
        PrincipalName = IdentityText.Require(principalName, nameof(principalName));
        PrincipalType = principalType;

        if (principalType == SecurityPrincipalType.None)
        {
            throw new ArgumentException("SecurityPrincipalType is required.", nameof(principalType));
        }

        if (principalType == SecurityPrincipalType.Human)
        {
            PartyReference = IdentityText.Require(partyReference, nameof(partyReference));
            return;
        }

        if (!string.IsNullOrWhiteSpace(partyReference))
        {
            throw new ArgumentException("Only human principals may reference Party.", nameof(partyReference));
        }
    }

    public string PrincipalName { get; }

    public SecurityPrincipalType PrincipalType { get; }

    public string? PartyReference { get; }

    public IReadOnlyCollection<PrincipalRoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    public PrincipalRoleAssignment AssignRole(
        Role role,
        AuthorizationScopeType scopeType,
        string scopeCode,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_roleAssignments.Any(assignment =>
            assignment.EffectiveToUtc is null
            && assignment.MatchesActiveScope(this, role, scopeType, scopeCode)))
        {
            throw new InvalidOperationException(
                "PrincipalRoleAssignment cannot contain duplicate active Principal/Role/Scope assignments.");
        }

        PrincipalRoleAssignment roleAssignment = new(this, role, scopeType, scopeCode, effectiveFromUtc, effectiveToUtc);
        _roleAssignments.Add(roleAssignment);
        return roleAssignment;
    }
}

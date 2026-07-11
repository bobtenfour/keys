using KeyInventory.Domain.Identity;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class IdentityDomainInvariantTests
{
    [Fact]
    public void HumanPrincipalRequiresPartyReference()
    {
        Assert.Throws<ArgumentException>(() => new SecurityPrincipal(
            "security.user",
            SecurityPrincipalType.Human,
            partyReference: null));
    }

    [Theory]
    [InlineData(SecurityPrincipalType.System)]
    [InlineData(SecurityPrincipalType.Integration)]
    public void NonHumanPrincipalCannotReferenceParty(SecurityPrincipalType principalType)
    {
        Assert.Throws<ArgumentException>(() => new SecurityPrincipal(
            "technical.principal",
            principalType,
            partyReference: "party-1"));
    }

    [Fact]
    public void RolePermissionRejectsDuplicatePermission()
    {
        Role role = new("org-1", "security-admin");
        Permission permission = new("identity.principal.read");

        role.AddPermission(permission);

        Assert.Throws<InvalidOperationException>(() => role.AddPermission(permission));
    }

    [Fact]
    public void PrincipalRoleAssignmentRejectsDuplicateActiveScope()
    {
        SecurityPrincipal principal = new("security.user", SecurityPrincipalType.Human, "party-1");
        Role role = new("org-1", "security-admin");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        principal.AssignRole(role, AuthorizationScopeType.Organization, "org-1", now, effectiveToUtc: null);

        Assert.Throws<InvalidOperationException>(() =>
            principal.AssignRole(role, AuthorizationScopeType.Organization, "org-1", now, effectiveToUtc: null));
    }

    [Fact]
    public void PrincipalRoleAssignmentRequiresEffectiveToAfterEffectiveFrom()
    {
        SecurityPrincipal principal = new("security.user", SecurityPrincipalType.Human, "party-1");
        Role role = new("org-1", "security-admin");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            principal.AssignRole(role, AuthorizationScopeType.Organization, "org-1", now, now));
    }

}

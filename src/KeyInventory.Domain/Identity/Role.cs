namespace KeyInventory.Domain.Identity;

public sealed class Role
{
    private readonly HashSet<string> _permissionCodes = new(StringComparer.Ordinal);
    private readonly List<RolePermission> _permissions = [];

    public Role(string roleCode)
    {
        RoleCode = IdentityText.Require(roleCode, nameof(roleCode));
    }

    public string RoleCode { get; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public RolePermission AddPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (!_permissionCodes.Add(permission.PermissionCode))
        {
            throw new InvalidOperationException("RolePermission cannot contain duplicate Role/Permission pairs.");
        }

        RolePermission rolePermission = new(this, permission);
        _permissions.Add(rolePermission);
        return rolePermission;
    }
}

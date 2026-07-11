namespace KeyInventory.Domain.Identity;

public sealed class RolePermission
{
    public RolePermission(Role role, Permission permission)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public Role Role { get; }

    public Permission Permission { get; }
}

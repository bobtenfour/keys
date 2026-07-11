namespace KeyInventory.Domain.Identity;

public sealed class Permission
{
    public Permission(string permissionCode)
    {
        PermissionCode = IdentityText.Require(permissionCode, nameof(permissionCode));
    }

    public string PermissionCode { get; }
}

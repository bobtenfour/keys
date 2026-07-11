using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public interface IRolePermissionService
{
    ValueTask<Role?> FindRoleAsync(
        string organizationCode,
        string roleCode,
        CancellationToken cancellationToken);

    ValueTask<Permission?> FindPermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken);
}

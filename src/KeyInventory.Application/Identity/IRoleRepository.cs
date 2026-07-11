using KeyInventory.Domain.Identity;

namespace KeyInventory.Application.Identity;

public interface IRoleRepository
{
    ValueTask<Role?> FindRoleAsync(
        string organizationCode,
        string roleCode,
        CancellationToken cancellationToken);

    ValueTask<Permission?> FindPermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken);
}

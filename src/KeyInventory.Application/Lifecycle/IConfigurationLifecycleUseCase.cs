namespace KeyInventory.Application.Lifecycle;

public interface IConfigurationLifecycleUseCase
{
    Task<IReadOnlyList<DepartmentLifecycleItem>> ListDepartmentsAsync(CancellationToken cancellationToken);

    Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task ActivateDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task RetireDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomLifecycleItem>> ListRoomsAsync(CancellationToken cancellationToken);

    Task DeleteRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task ActivateRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task RetireRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberLifecycleItem>> ListWorkforceMembersAsync(
        CancellationToken cancellationToken);

    Task DeleteWorkforceMemberAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkAssignmentLifecycleItem>> ListWorkAssignmentsAsync(
        CancellationToken cancellationToken);

    Task DeleteWorkAssignmentAsync(string workAssignmentCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyTypeLifecycleItem>> ListKeyTypesAsync(CancellationToken cancellationToken);

    Task DeleteKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task ActivateKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task RetireKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAssetLifecycleItem>> ListKeyAssetsAsync(CancellationToken cancellationToken);

    Task DeleteKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task ActivateKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task RetireKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAccessPatternLifecycleItem>> ListKeyAccessPatternsAsync(
        CancellationToken cancellationToken);

    Task ActivateKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);

    Task RetireKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);

    Task DeleteKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);
}

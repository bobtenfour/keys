using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Application.Workforce;

namespace KeyInventory.Application.Readiness;

public sealed record OperationalReadinessSnapshot(
    bool HasDepartment,
    bool HasRoom,
    bool HasWorkforceMember,
    bool HasWorkAssignment,
    bool HasKey,
    bool HasValidKeyAccess,
    bool CanIssueKey,
    int DepartmentCount,
    int RoomCount,
    int WorkforceMemberCount,
    int WorkAssignmentCount,
    int KeyCount);

public interface IOperationalReadinessUseCase
{
    Task<OperationalReadinessSnapshot> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class OperationalReadinessUseCase : IOperationalReadinessUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IKeyAccessResolutionPort _accessResolution;

    public OperationalReadinessUseCase(
        IWorkforcePersistencePort workforce,
        IKeyCatalogPersistencePort catalog,
        IKeyAccessResolutionPort accessResolution)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _accessResolution = accessResolution ?? throw new ArgumentNullException(nameof(accessResolution));
    }

    public async Task<OperationalReadinessSnapshot> ExecuteAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItem> departments = await _workforce
            .ListDepartmentsAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RoomListItem> rooms = await _workforce.ListRoomsAsync(cancellationToken)
            .ConfigureAwait(false);
        int workforceMemberCount = await _workforce.CountWorkforceMembersAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<WorkAssignmentListItem> workAssignments = await _workforce
            .ListWorkAssignmentsAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<KeyAssetListItem> keys = await _catalog.ListKeyAssetsAsync(cancellationToken)
            .ConfigureAwait(false);
        bool hasValidKeyAccess = await _accessResolution
            .HasValidKeyAccessAsync(cancellationToken)
            .ConfigureAwait(false);

        bool hasDepartment = departments.Count > 0;
        bool hasRoom = rooms.Count > 0;
        bool hasWorkforceMember = workforceMemberCount > 0;
        bool hasWorkAssignment = workAssignments.Count > 0;
        bool hasKey = keys.Count > 0;
        bool canIssueKey = hasWorkforceMember && hasWorkAssignment && hasKey;

        return new OperationalReadinessSnapshot(
            hasDepartment,
            hasRoom,
            hasWorkforceMember,
            hasWorkAssignment,
            hasKey,
            hasValidKeyAccess,
            canIssueKey,
            departments.Count,
            rooms.Count,
            workforceMemberCount,
            workAssignments.Count,
            keys.Count);
    }
}

using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Lifecycle;

public sealed class ConfigurationLifecycleUseCase : IConfigurationLifecycleUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;
    private readonly IOperatorAuditRecorder _audit;
    private readonly IKeyAccessPatternRoomAssignmentPersistencePort _roomAssignments;
    private readonly IActivateDepartmentUseCase _activateDepartment;
    private readonly IRetireDepartmentUseCase _retireDepartment;
    private readonly IActivateRoomUseCase _activateRoom;
    private readonly IRetireRoomUseCase _retireRoom;
    private readonly IActivateKeyTypeUseCase _activateKeyType;
    private readonly IRetireKeyTypeUseCase _retireKeyType;

    public ConfigurationLifecycleUseCase(
        IWorkforcePersistencePort workforce,
        IKeyCatalogPersistencePort catalog,
        ILoanPersistencePort loans,
        IOperatorAuditRecorder audit,
        IKeyAccessPatternRoomAssignmentPersistencePort roomAssignments,
        IActivateDepartmentUseCase activateDepartment,
        IRetireDepartmentUseCase retireDepartment,
        IActivateRoomUseCase activateRoom,
        IRetireRoomUseCase retireRoom,
        IActivateKeyTypeUseCase activateKeyType,
        IRetireKeyTypeUseCase retireKeyType)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _roomAssignments = roomAssignments ?? throw new ArgumentNullException(nameof(roomAssignments));
        _activateDepartment = activateDepartment ?? throw new ArgumentNullException(nameof(activateDepartment));
        _retireDepartment = retireDepartment ?? throw new ArgumentNullException(nameof(retireDepartment));
        _activateRoom = activateRoom ?? throw new ArgumentNullException(nameof(activateRoom));
        _retireRoom = retireRoom ?? throw new ArgumentNullException(nameof(retireRoom));
        _activateKeyType = activateKeyType ?? throw new ArgumentNullException(nameof(activateKeyType));
        _retireKeyType = retireKeyType ?? throw new ArgumentNullException(nameof(retireKeyType));
    }

    public async Task<IReadOnlyList<DepartmentLifecycleItem>> ListDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItem> departments = await _workforce
            .ListDepartmentsAsync(cancellationToken)
            .ConfigureAwait(false);

        List<DepartmentLifecycleItem> items = new(departments.Count);
        foreach (DepartmentListItem department in departments)
        {
            (bool canDelete, string? blockedReason) = await EvaluateDepartmentDeleteAsync(
                    department.DepartmentId,
                    cancellationToken)
                .ConfigureAwait(false);

            items.Add(new DepartmentLifecycleItem(
                department.DepartmentId,
                department.DepartmentCode,
                department.IsActive,
                new LifecycleCapabilities(
                    CanEdit: true,
                    CanDelete: canDelete,
                    CanRetire: department.IsActive && !canDelete,
                    CanActivate: !department.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        Department? department = await _workforce.FindDepartmentAsync(departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        (bool canDelete, string? blockedReason) = await EvaluateDepartmentDeleteAsync(
                department.DepartmentId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This department can no longer be deleted because it is in use. Retire it instead to preserve its history.");
        }

        _audit.Stage(
            OperatorAuditActions.DepartmentDeleted,
            OperatorAuditSubjects.Department,
            department.DepartmentCode,
            $"DepartmentId={department.DepartmentId:D}");
        await _workforce.DeleteDepartmentAsync(department.DepartmentId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task ActivateDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
        => _activateDepartment.ExecuteAsync(departmentId, cancellationToken);

    public Task RetireDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
        => _retireDepartment.ExecuteAsync(departmentId, cancellationToken);

    public async Task<IReadOnlyList<RoomLifecycleItem>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomListItem> rooms = await _workforce.ListRoomsAsync(cancellationToken)
            .ConfigureAwait(false);

        List<RoomLifecycleItem> items = new(rooms.Count);
        foreach (RoomListItem room in rooms)
        {
            (bool canDelete, string? blockedReason) = await EvaluateRoomDeleteAsync(
                    room.RoomCode,
                    cancellationToken)
                .ConfigureAwait(false);

            items.Add(new RoomLifecycleItem(
                room.RoomCode,
                room.RoomNumber,
                room.Description,
                room.IsActive,
                new LifecycleCapabilities(
                    CanEdit: true,
                    CanDelete: canDelete,
                    CanRetire: room.IsActive,
                    CanActivate: !room.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        string code = roomCode.Trim();

        Domain.Locations.Room? room = await _workforce.FindRoomAsync(code, cancellationToken)
            .ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        (bool canDelete, string? blockedReason) = await EvaluateRoomDeleteAsync(code, cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This room can no longer be deleted because it is in use. Retire it instead to preserve its history.");
        }

        _audit.Stage(
            OperatorAuditActions.RoomDeleted,
            OperatorAuditSubjects.Room,
            room.RoomCode);
        await _workforce.DeleteRoomAsync(room.RoomCode, cancellationToken).ConfigureAwait(false);
    }

    public Task ActivateRoomAsync(string roomCode, CancellationToken cancellationToken)
        => _activateRoom.ExecuteAsync(roomCode, cancellationToken);

    public Task RetireRoomAsync(string roomCode, CancellationToken cancellationToken)
        => _retireRoom.ExecuteAsync(roomCode, cancellationToken);

    public async Task<IReadOnlyList<WorkforceMemberLifecycleItem>> ListWorkforceMembersAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkforceMemberListItem> members = await _workforce
            .ListWorkforceMembersAsync(cancellationToken)
            .ConfigureAwait(false);

        List<WorkforceMemberLifecycleItem> items = new(members.Count);
        foreach (WorkforceMemberListItem member in members)
        {
            (bool canDelete, string? blockedReason) = await EvaluateWorkforceMemberDeleteAsync(
                    member.WorkforceMemberCode,
                    member.PartyCode,
                    cancellationToken)
                .ConfigureAwait(false);

            bool isActive = string.Equals(
                member.Status,
                nameof(WorkforceMemberStatus.Active),
                StringComparison.Ordinal);

            items.Add(new WorkforceMemberLifecycleItem(
                member.WorkforceMemberCode,
                member.PartyCode,
                member.FirstName,
                member.LastName,
                member.Uin,
                member.WorkforceType,
                member.DepartmentCode,
                member.Status,
                new LifecycleCapabilities(
                    CanEdit: isActive,
                    CanDelete: canDelete,
                    CanRetire: false,
                    CanActivate: false,
                    CanTerminate: isActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);
        string code = workforceMemberCode.Trim();

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(code, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        (bool canDelete, string? blockedReason) = await EvaluateWorkforceMemberDeleteAsync(
                member.WorkforceMemberCode,
                member.PartyCode,
                cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This workforce member can no longer be deleted because they are in use. Terminate them instead to preserve history.");
        }

        string partyCode = member.PartyCode;
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberDeleted,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode);
        await _workforce.DeleteWorkforceMemberAsync(member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        int remainingMembers = await _workforce
            .CountWorkforceMembersForPartyAsync(partyCode, cancellationToken)
            .ConfigureAwait(false);
        int partyLoans = await _loans.CountLoansForPartyAsync(partyCode, cancellationToken)
            .ConfigureAwait(false);
        if (remainingMembers == 0 && partyLoans == 0)
        {
            await _workforce.DeletePartyAsync(partyCode, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<WorkAssignmentLifecycleItem>> ListWorkAssignmentsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkAssignmentListItem> assignments = await _workforce
            .ListWorkAssignmentsAsync(cancellationToken)
            .ConfigureAwait(false);

        List<WorkAssignmentLifecycleItem> items = new(assignments.Count);
        foreach (WorkAssignmentListItem assignment in assignments)
        {
            (bool canDelete, string? blockedReason) = await EvaluateWorkAssignmentDeleteAsync(
                    assignment,
                    cancellationToken)
                .ConfigureAwait(false);

            items.Add(new WorkAssignmentLifecycleItem(
                assignment.WorkAssignmentCode,
                assignment.WorkforceMemberCode,
                assignment.RoomCode,
                assignment.IsPrimary,
                assignment.IsActive,
                new LifecycleCapabilities(
                    CanEdit: false,
                    CanDelete: canDelete,
                    CanRetire: false,
                    CanActivate: false,
                    CanEnd: assignment.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteWorkAssignmentAsync(
        string workAssignmentCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workAssignmentCode);
        string code = workAssignmentCode.Trim();

        WorkAssignment? assignment = await _workforce.FindWorkAssignmentAsync(code, cancellationToken)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            throw new InvalidOperationException("The work assignment was not found.");
        }

        WorkAssignmentListItem snapshot = new(
            assignment.WorkAssignmentCode,
            assignment.WorkforceMemberCode,
            assignment.RoomCode,
            assignment.IsPrimary,
            assignment.IsActive);

        (bool canDelete, string? blockedReason) = await EvaluateWorkAssignmentDeleteAsync(
                snapshot,
                cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This work assignment can no longer be deleted because it has historical significance. End it instead to preserve history.");
        }

        _audit.Stage(
            OperatorAuditActions.WorkAssignmentDeleted,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentCode);
        await _workforce.DeleteWorkAssignmentAsync(assignment.WorkAssignmentCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<KeyTypeLifecycleItem>> ListKeyTypesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyTypeListItem> types = await _catalog.ListKeyTypesAsync(cancellationToken)
            .ConfigureAwait(false);

        List<KeyTypeLifecycleItem> items = new(types.Count);
        foreach (KeyTypeListItem keyType in types)
        {
            int allPatterns = await _catalog
                .CountAllKeyAccessPatternsForTypeAsync(keyType.TypeCode, cancellationToken)
                .ConfigureAwait(false);
            (bool canDelete, string? blockedReason) = EvaluateKeyTypeDelete(allPatterns);

            items.Add(new KeyTypeLifecycleItem(
                keyType.TypeCode,
                keyType.IsActive,
                keyType.ActiveKeyAssetCount,
                new LifecycleCapabilities(
                    CanEdit: false,
                    CanDelete: canDelete,
                    CanRetire: keyType.IsActive && keyType.ActiveKeyAssetCount == 0,
                    CanActivate: !keyType.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);
        string code = typeCode.Trim();

        KeyType? keyType = await _catalog.FindKeyTypeAsync(code, cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            throw new InvalidOperationException("The key type was not found.");
        }

        int allPatterns = await _catalog
            .CountAllKeyAccessPatternsForTypeAsync(keyType.TypeCode, cancellationToken)
            .ConfigureAwait(false);
        (bool canDelete, string? blockedReason) = EvaluateKeyTypeDelete(allPatterns);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This key type can no longer be deleted because it is referenced by KEY # access patterns. Retire it instead to preserve history.");
        }

        _audit.Stage(
            OperatorAuditActions.KeyTypeDeleted,
            OperatorAuditSubjects.KeyType,
            keyType.TypeCode);
        await _catalog.DeleteKeyTypeAsync(keyType.TypeCode, cancellationToken).ConfigureAwait(false);
    }

    public Task ActivateKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
        => _activateKeyType.ExecuteAsync(typeCode, cancellationToken);

    public Task RetireKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
        => _retireKeyType.ExecuteAsync(typeCode, cancellationToken);

    public async Task<IReadOnlyList<KeyAssetLifecycleItem>> ListKeyAssetsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAssetListItem> assets = await _catalog.ListKeyAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        List<KeyAssetLifecycleItem> items = new(assets.Count);
        foreach (KeyAssetListItem asset in assets)
        {
            bool isIssued = await _loans
                .HasOpenLoanForKeyAssetAsync(asset.KeyAssetId, cancellationToken)
                .ConfigureAwait(false);
            (bool canDelete, string? blockedReason) = await EvaluateKeyAssetDeleteAsync(
                    asset.KeyAssetId,
                    cancellationToken)
                .ConfigureAwait(false);

            items.Add(new KeyAssetLifecycleItem(
                asset.KeyAssetId,
                asset.KeyNumber,
                asset.MedecoKeyCode,
                asset.TypeCode,
                asset.IsActive,
                isIssued ? OperationalKeyAvailability.Issued : OperationalKeyAvailability.Available,
                new LifecycleCapabilities(
                    CanEdit: false,
                    CanDelete: canDelete,
                    CanRetire: asset.IsActive,
                    CanActivate: !asset.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task DeleteKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The physical key copy was not found.");
        }

        (bool canDelete, string? blockedReason) = await EvaluateKeyAssetDeleteAsync(
                keyAsset.KeyAssetId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This physical key copy can no longer be deleted because it has loan history. Retire it instead to preserve history.");
        }

        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyDeleted,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}");
        await _catalog.DeleteKeyAssetAsync(keyAsset.KeyAssetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task ActivateKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The physical key copy was not found.");
        }

        keyAsset.Activate();
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyActivated,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}");
        await _catalog.UpdateKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }

    public async Task RetireKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The physical key copy was not found.");
        }

        keyAsset.Retire();
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRetired,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}");
        await _catalog.UpdateKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<KeyAccessPatternLifecycleItem>> ListKeyAccessPatternsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAccessPatternListItem> patterns = await _catalog
            .ListKeyAccessPatternsAsync(cancellationToken)
            .ConfigureAwait(false);

        List<KeyAccessPatternLifecycleItem> items = new(patterns.Count);
        foreach (KeyAccessPatternListItem pattern in patterns)
        {
            (bool canDelete, string? blockedReason) = await EvaluateKeyAccessPatternDeleteAsync(
                    pattern.KeyNumber,
                    cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<KeyAssetListItem> copies = await _catalog
                .ListKeyAssetsForPatternAsync(pattern.KeyNumber, cancellationToken)
                .ConfigureAwait(false);
            bool hasActivePhysicalCopies = copies.Any(copy => copy.IsActive);

            items.Add(new KeyAccessPatternLifecycleItem(
                pattern.KeyNumber,
                pattern.TypeCode,
                pattern.IsActive,
                pattern.PhysicalCopyCount,
                new LifecycleCapabilities(
                    CanEdit: false,
                    CanDelete: canDelete,
                    CanRetire: pattern.IsActive && !hasActivePhysicalCopies,
                    CanActivate: !pattern.IsActive,
                    DeleteBlockedReason: blockedReason)));
        }

        return items;
    }

    public async Task ActivateKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        string number = keyNumber.Trim();

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(number, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            throw new InvalidOperationException("The KEY # was not found.");
        }

        pattern.Activate();
        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternActivated,
            OperatorAuditSubjects.KeyAccessPattern,
            pattern.KeyNumber);
        await _catalog.UpdateKeyAccessPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
    }

    public async Task RetireKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        string number = keyNumber.Trim();

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(number, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            throw new InvalidOperationException("The KEY # was not found.");
        }

        IReadOnlyList<KeyAssetListItem> copies = await _catalog
            .ListKeyAssetsForPatternAsync(pattern.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        bool hasActivePhysicalCopies = copies.Any(copy => copy.IsActive);
        pattern.Retire(hasActivePhysicalCopies);
        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternRetired,
            OperatorAuditSubjects.KeyAccessPattern,
            pattern.KeyNumber);
        await _catalog.UpdateKeyAccessPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        string number = keyNumber.Trim();

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(number, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            throw new InvalidOperationException("The KEY # was not found.");
        }

        (bool canDelete, string? blockedReason) = await EvaluateKeyAccessPatternDeleteAsync(
                pattern.KeyNumber,
                cancellationToken)
            .ConfigureAwait(false);
        if (!canDelete)
        {
            throw new InvalidOperationException(
                "This KEY # can no longer be deleted because it still has physical copies or room assignments. Remove those relationships or retire it to preserve history.");
        }

        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternDeleted,
            OperatorAuditSubjects.KeyAccessPattern,
            pattern.KeyNumber);
        await _catalog.DeleteKeyAccessPatternAsync(pattern.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateDepartmentDeleteAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        int memberCount = await _workforce
            .CountWorkforceMembersForDepartmentAsync(departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (memberCount > 0)
        {
            return (false, "Department is referenced by workforce members.");
        }

        int justifiedLoanCount = await _loans
            .CountLoansJustifiedByDepartmentAsync(departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (justifiedLoanCount > 0)
        {
            return (false, "Department appears in key issue history.");
        }

        return (true, null);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateRoomDeleteAsync(
        string roomCode,
        CancellationToken cancellationToken)
    {
        int assignmentCount = await _workforce
            .CountWorkAssignmentsForRoomAsync(roomCode, cancellationToken)
            .ConfigureAwait(false);
        if (assignmentCount > 0)
        {
            return (false, "Room is referenced by work assignments.");
        }

        IReadOnlyList<string> keyNumbers = await _roomAssignments
            .ListKeyNumbersForRoomAsync(roomCode, cancellationToken)
            .ConfigureAwait(false);
        if (keyNumbers.Count > 0)
        {
            return (false, "Room is referenced by KEY #↔Room assignments.");
        }

        int justifiedLoanCount = await _loans
            .CountLoansJustifiedByRoomAsync(roomCode, cancellationToken)
            .ConfigureAwait(false);
        if (justifiedLoanCount > 0)
        {
            return (false, "Room appears in key issue history.");
        }

        return (true, null);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateWorkforceMemberDeleteAsync(
        string workforceMemberCode,
        string partyCode,
        CancellationToken cancellationToken)
    {
        int assignmentCount = await _workforce
            .CountWorkAssignmentsForMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (assignmentCount > 0)
        {
            return (false, "Workforce member has work assignments.");
        }

        int loanCount = await _loans.CountLoansForPartyAsync(partyCode, cancellationToken)
            .ConfigureAwait(false);
        if (loanCount > 0)
        {
            return (false, "Workforce member's party has loan history.");
        }

        return (true, null);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateWorkAssignmentDeleteAsync(
        WorkAssignmentListItem assignment,
        CancellationToken cancellationToken)
    {
        if (!assignment.IsActive)
        {
            return (false, "Ended work assignments are historical and cannot be deleted.");
        }

        WorkforceMember? member = await _workforce
            .FindWorkforceMemberAsync(assignment.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            return (false, "The workforce member for this assignment was not found.");
        }

        int loanCount = await _loans.CountLoansForPartyAsync(member.PartyCode, cancellationToken)
            .ConfigureAwait(false);
        if (loanCount > 0)
        {
            return (false, "Member has loan history; end the assignment instead of deleting.");
        }

        return (true, null);
    }

    private static (bool CanDelete, string? BlockedReason) EvaluateKeyTypeDelete(int allPatternCount)
    {
        if (allPatternCount > 0)
        {
            return (false, "Key type is referenced by KEY # access patterns.");
        }

        return (true, null);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateKeyAssetDeleteAsync(
        Guid keyAssetId,
        CancellationToken cancellationToken)
    {
        int loanCount = await _loans.CountLoansForKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (loanCount > 0)
        {
            return (false, "Physical key copy has loan history.");
        }

        return (true, null);
    }

    private async Task<(bool CanDelete, string? BlockedReason)> EvaluateKeyAccessPatternDeleteAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        int assetCount = await _catalog
            .CountKeyAssetsForKeyNumberAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (assetCount > 0)
        {
            return (false, "KEY # has physical key copies.");
        }

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyNumberAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (rooms.Count > 0)
        {
            return (false, "KEY # has room assignments.");
        }

        return (true, null);
    }
}

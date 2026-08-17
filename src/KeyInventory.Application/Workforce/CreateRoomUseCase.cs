using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateRoomUseCase
{
    /// <summary>
    /// Creates a Room with a system-generated RoomCode belonging to the given Department.
    /// The Department reference can be supplied by DepartmentId (preferred) or DepartmentCode.
    /// </summary>
    Task<string> ExecuteAsync(
        Guid departmentId,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves DepartmentCode to DepartmentId and delegates to the DepartmentId overload.
    /// </summary>
    Task<string> ExecuteAsync(
        string departmentCode,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken);
}

public interface IListRoomsUseCase
{
    Task<IReadOnlyList<RoomListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateRoomUseCase : ICreateRoomUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateRoomUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<string> ExecuteAsync(
        Guid departmentId,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        Department? department = await _workforce.FindDepartmentAsync(departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("The department was not found or is inactive.");
        }

        string roomCode = WorkforceIdentityCodes.NewRoomCode();
        Room room = new(roomCode, roomNumber, department.DepartmentId, description);

        if (await _workforce.RoomNumberExistsAsync(room.RoomNumber, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("RoomNumber must be globally unique.");
        }

        _audit.Stage(
            OperatorAuditActions.RoomCreated,
            OperatorAuditSubjects.Room,
            room.RoomCode,
            $"RoomNumber={room.RoomNumber}; DepartmentId={department.DepartmentId:D}; Department={department.DepartmentCode}");
        await _workforce.AddRoomAsync(room, cancellationToken).ConfigureAwait(false);
        return roomCode;
    }

    public async Task<string> ExecuteAsync(
        string departmentCode,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        Department? department = await _workforce
            .FindDepartmentByCodeAsync(departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        return await ExecuteAsync(department.DepartmentId, roomNumber, description, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ListRoomsUseCase : IListRoomsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListRoomsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<RoomListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListRoomsAsync(cancellationToken);
    }
}

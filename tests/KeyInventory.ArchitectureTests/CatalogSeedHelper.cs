using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.ArchitectureTests;

internal static class CatalogSeedHelper
{
    /// <summary>
    /// Registers a key under a KEY #. Application resolves whether the KEY # exists:
    /// creates a new KEY # atomically with Classification/Room and first key when needed,
    /// otherwise registers an additional key under the existing KEY #.
    /// When creating a new Regular KEY # without a Room, a disposable Room is created.
    /// </summary>
    public static Task CreatePhysicalKeyAsync(
        IServiceProvider services,
        string keyNumber,
        string medecoKeyCode,
        KeyAccessClassification classification,
        CancellationToken cancellationToken)
        => CreatePhysicalKeyAsync(
            services,
            keyNumber,
            medecoKeyCode,
            classification,
            roomCode: null,
            cancellationToken);

    /// <summary>
    /// Registers a key under a KEY #. Application resolves whether the KEY # exists:
    /// creates a new KEY # atomically with Classification/Room and first key when needed,
    /// otherwise registers an additional key under the existing KEY #.
    /// When creating a new Regular KEY # without <paramref name="roomCode"/>, a disposable Room is created.
    /// </summary>
    public static async Task CreatePhysicalKeyAsync(
        IServiceProvider services,
        string keyNumber,
        string medecoKeyCode,
        KeyAccessClassification classification = KeyAccessClassification.Regular,
        string? roomCode = null,
        CancellationToken cancellationToken = default)
    {
        IGetKeyNumberRegistrationPreviewUseCase preview =
            services.GetRequiredService<IGetKeyNumberRegistrationPreviewUseCase>();
        KeyNumberRegistrationPreview? existing =
            await preview.ExecuteAsync(keyNumber, cancellationToken).ConfigureAwait(false);

        ICreateKeyAssetUseCase createKey = services.GetRequiredService<ICreateKeyAssetUseCase>();
        if (existing is null)
        {
            IReadOnlyList<string> rooms;
            if (classification == KeyAccessClassification.Master)
            {
                rooms = [];
            }
            else if (!string.IsNullOrWhiteSpace(roomCode))
            {
                rooms = [roomCode.Trim()];
            }
            else
            {
                string ensuredRoom = await EnsureDisposableRoomAsync(services, keyNumber, cancellationToken)
                    .ConfigureAwait(false);
                rooms = [ensuredRoom];
            }

            await createKey
                .RegisterNewKeyAsync(keyNumber, medecoKeyCode, classification, rooms, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await createKey
                .RegisterNewKeyAsync(keyNumber, medecoKeyCode, classification: null, roomCodes: null, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a Room with a generated RoomCode under the given Department. Returns the RoomCode.
    /// </summary>
    public static Task<string> CreateRoomAsync(
        IServiceProvider services,
        Guid departmentId,
        string roomNumber,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        ICreateRoomUseCase create = services.GetRequiredService<ICreateRoomUseCase>();
        return create.ExecuteAsync(departmentId, roomNumber, description, cancellationToken);
    }

    /// <summary>
    /// Creates a Room under the department resolved by <paramref name="departmentCode"/>. Returns the RoomCode.
    /// </summary>
    public static Task<string> CreateRoomByDepartmentCodeAsync(
        IServiceProvider services,
        string departmentCode,
        string roomNumber,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        ICreateRoomUseCase create = services.GetRequiredService<ICreateRoomUseCase>();
        return create.ExecuteAsync(departmentCode, roomNumber, description, cancellationToken);
    }

    private static async Task<string> EnsureDisposableRoomAsync(
        IServiceProvider services,
        string keyNumber,
        CancellationToken cancellationToken)
    {
        IListDepartmentsUseCase listDepartments = services.GetRequiredService<IListDepartmentsUseCase>();
        IReadOnlyList<DepartmentListItem> departments = await listDepartments
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        string departmentCode;
        if (departments.Count == 0)
        {
            ICreateDepartmentUseCase createDepartment = services.GetRequiredService<ICreateDepartmentUseCase>();
            departmentCode = $"auto-dept-{Guid.NewGuid():N}"[..20];
            await createDepartment.ExecuteAsync(departmentCode, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            departmentCode = departments[0].DepartmentCode;
        }

        string roomNumber = $"AUTO-{Guid.NewGuid():N}"[..20];
        return await CreateRoomByDepartmentCodeAsync(
                services,
                departmentCode,
                roomNumber,
                "Auto-seeded Regular KEY # room",
                cancellationToken)
            .ConfigureAwait(false);
    }
}

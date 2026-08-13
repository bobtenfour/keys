namespace KeyInventory.Infrastructure.Data;

public sealed class LoanEntity
{
    public string LoanCode { get; set; } = string.Empty;

    public Guid KeyAssetId { get; set; }

    public string BorrowerPartyReference { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset DueAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? JustificationKind { get; set; }

    public Guid? JustificationDepartmentId { get; set; }

    public string? JustificationDepartmentCodeSnapshot { get; set; }

    public string? JustificationRoomCode { get; set; }

    public KeyAssetEntity KeyAsset { get; set; } = null!;

    public PartyEntity BorrowerParty { get; set; } = null!;

    public DepartmentEntity? JustificationDepartment { get; set; }

    public RoomEntity? JustificationRoom { get; set; }
}

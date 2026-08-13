namespace KeyInventory.Infrastructure.Data;

public sealed class WorkforceMemberEntity
{
    public string WorkforceMemberCode { get; set; } = string.Empty;

    public string PartyCode { get; set; } = string.Empty;

    public string WorkforceType { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public PartyEntity Party { get; set; } = null!;

    public DepartmentEntity Department { get; set; } = null!;
}

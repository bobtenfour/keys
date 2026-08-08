namespace KeyInventory.Infrastructure.Data;

public sealed class WorkforceMemberEntity
{
    public string WorkforceMemberCode { get; set; } = string.Empty;

    public string PartyCode { get; set; } = string.Empty;

    public string WorkforceType { get; set; } = string.Empty;

    public string OrganizationCode { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public string ResponsibleManagerWorkforceMemberCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public PartyEntity Party { get; set; } = null!;
}

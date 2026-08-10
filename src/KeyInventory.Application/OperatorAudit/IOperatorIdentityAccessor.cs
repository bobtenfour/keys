namespace KeyInventory.Application.OperatorAudit;

public interface IOperatorIdentityAccessor
{
    /// <summary>
    /// Returns the authenticated KeyInventory user name. Distinct from WorkforceMember identity.
    /// </summary>
    string GetRequiredOperatorReference();
}

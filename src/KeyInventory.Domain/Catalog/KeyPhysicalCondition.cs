namespace KeyInventory.Domain.Catalog;

/// <summary>
/// Authoritative physical condition of one KeyAsset (individual key).
/// Available/Issued are derived from Condition plus open Loan — not Condition values.
/// </summary>
public enum KeyPhysicalCondition
{
    Active = 0,
    Lost = 1,
    Destroyed = 2
}

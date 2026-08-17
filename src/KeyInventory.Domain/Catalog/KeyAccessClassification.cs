namespace KeyInventory.Domain.Catalog;

/// <summary>
/// Explicit KEY # classification. Sole classification authority for KeyAccessPattern.
/// Not inferred from Room count, KEY # text, MEDECO, holder, or Department.
/// Regular KEY # opens exactly one Room; Master KEY # opens all Rooms.
/// </summary>
public enum KeyAccessClassification
{
    Regular = 0,
    Master = 1
}

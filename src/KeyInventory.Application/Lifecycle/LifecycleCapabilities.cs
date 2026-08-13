namespace KeyInventory.Application.Lifecycle;

public sealed record LifecycleCapabilities(
    bool CanEdit,
    bool CanDelete,
    bool CanRetire,
    bool CanActivate,
    bool CanEnd = false,
    bool CanTerminate = false,
    bool CanRemove = false,
    string? DeleteBlockedReason = null);

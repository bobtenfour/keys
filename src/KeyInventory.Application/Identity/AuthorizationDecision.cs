namespace KeyInventory.Application.Identity;

public sealed record AuthorizationDecision(bool IsAuthorized, string? ReasonCode);

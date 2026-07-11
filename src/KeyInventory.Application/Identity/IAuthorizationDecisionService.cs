namespace KeyInventory.Application.Identity;

public interface IAuthorizationDecisionService
{
    ValueTask<AuthorizationDecision> DecideAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken);
}

using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateOrganizationUseCase
{
    Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken);
}

public interface IListOrganizationsUseCase
{
    Task<IReadOnlyList<OrganizationListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateOrganizationUseCase : ICreateOrganizationUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public CreateOrganizationUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken)
    {
        if (await _workforce.OrganizationExistsAsync(organizationCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("An organization with this code already exists.");
        }

        await _workforce.AddOrganizationAsync(new Organization(organizationCode), cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListOrganizationsUseCase : IListOrganizationsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListOrganizationsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<OrganizationListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListOrganizationsAsync(cancellationToken);
    }
}

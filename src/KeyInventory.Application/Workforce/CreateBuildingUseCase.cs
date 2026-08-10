using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Workforce;

public interface ICreateBuildingUseCase
{
    Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken);
}

public interface IListBuildingsUseCase
{
    Task<IReadOnlyList<BuildingListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateBuildingUseCase : ICreateBuildingUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateBuildingUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken)
    {
        if (await _workforce.BuildingExistsAsync(buildingCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A building with this code already exists.");
        }

        Building building = new(buildingCode);
        _audit.Stage(
            OperatorAuditActions.BuildingCreated,
            OperatorAuditSubjects.Building,
            building.BuildingCode);
        await _workforce.AddBuildingAsync(building, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListBuildingsUseCase : IListBuildingsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListBuildingsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<BuildingListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListBuildingsAsync(cancellationToken);
    }
}

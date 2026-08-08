using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateWorkforceMemberUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string partyCode,
        string workforceType,
        string organizationCode,
        string departmentCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken);
}

public interface ICreateBootstrapWorkforcePairUseCase
{
    Task ExecuteAsync(
        string firstWorkforceMemberCode,
        string firstPartyCode,
        string firstWorkforceType,
        string secondWorkforceMemberCode,
        string secondPartyCode,
        string secondWorkforceType,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken);
}

public interface IListWorkforceMembersUseCase
{
    Task<IReadOnlyList<WorkforceMemberListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public interface ITerminateWorkforceMemberUseCase
{
    Task ExecuteAsync(string workforceMemberCode, CancellationToken cancellationToken);
}

public sealed class CreateWorkforceMemberUseCase : ICreateWorkforceMemberUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public CreateWorkforceMemberUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string partyCode,
        string workforceType,
        string organizationCode,
        string departmentCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken)
    {
        await EnsureMemberPrerequisitesAsync(
                workforceMemberCode,
                partyCode,
                organizationCode,
                departmentCode,
                cancellationToken)
            .ConfigureAwait(false);

        WorkforceMember? manager = await _workforce
            .FindWorkforceMemberAsync(responsibleManagerWorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (manager is null || manager.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("ResponsibleManager must be an existing Active WorkforceMember.");
        }

        WorkforceMember member = new(
            workforceMemberCode,
            partyCode,
            ParseWorkforceType(workforceType),
            organizationCode,
            departmentCode,
            responsibleManagerWorkforceMemberCode);

        await _workforce.AddWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureMemberPrerequisitesAsync(
        string workforceMemberCode,
        string partyCode,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        if (await _workforce.WorkforceMemberExistsAsync(workforceMemberCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A workforce member with this code already exists.");
        }

        Party? party = await _workforce.FindPartyAsync(partyCode, cancellationToken).ConfigureAwait(false);
        if (party is null || !party.IsActive)
        {
            throw new InvalidOperationException("The party was not found or is inactive.");
        }

        if (await _workforce.ActiveWorkforceMemberExistsForPartyAsync(party.PartyCode, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("A Party may have at most one Active WorkforceMember.");
        }

        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || !organization.IsActive)
        {
            throw new InvalidOperationException("The organization was not found or is inactive.");
        }

        Department? department = await _workforce
            .FindDepartmentAsync(organization.OrganizationCode, departmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("The department was not found or is inactive.");
        }
    }

    internal static WorkforceType ParseWorkforceType(string workforceType)
    {
        if (!Enum.TryParse(workforceType, ignoreCase: true, out WorkforceType parsed)
            || parsed is WorkforceType.None)
        {
            throw new InvalidOperationException("WorkforceType must be Employee or Contractor.");
        }

        return parsed;
    }
}

public sealed class CreateBootstrapWorkforcePairUseCase : ICreateBootstrapWorkforcePairUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public CreateBootstrapWorkforcePairUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(
        string firstWorkforceMemberCode,
        string firstPartyCode,
        string firstWorkforceType,
        string secondWorkforceMemberCode,
        string secondPartyCode,
        string secondWorkforceType,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        if (await _workforce.CountWorkforceMembersAsync(cancellationToken).ConfigureAwait(false) != 0)
        {
            throw new InvalidOperationException(
                "Bootstrap workforce pair may be created only when no WorkforceMember records exist.");
        }

        await ValidatePartyAsync(firstPartyCode, cancellationToken).ConfigureAwait(false);
        await ValidatePartyAsync(secondPartyCode, cancellationToken).ConfigureAwait(false);

        if (string.Equals(firstPartyCode, secondPartyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bootstrap workforce pair requires two different Party records.");
        }

        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || !organization.IsActive)
        {
            throw new InvalidOperationException("The organization was not found or is inactive.");
        }

        Department? department = await _workforce
            .FindDepartmentAsync(organization.OrganizationCode, departmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("The department was not found or is inactive.");
        }

        WorkforceMember first = new(
            firstWorkforceMemberCode,
            firstPartyCode,
            CreateWorkforceMemberUseCase.ParseWorkforceType(firstWorkforceType),
            organization.OrganizationCode,
            department.DepartmentCode,
            secondWorkforceMemberCode);
        WorkforceMember second = new(
            secondWorkforceMemberCode,
            secondPartyCode,
            CreateWorkforceMemberUseCase.ParseWorkforceType(secondWorkforceType),
            organization.OrganizationCode,
            department.DepartmentCode,
            firstWorkforceMemberCode);

        await _workforce.AddWorkforceMembersAsync([first, second], cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidatePartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        Party? party = await _workforce.FindPartyAsync(partyCode, cancellationToken).ConfigureAwait(false);
        if (party is null || !party.IsActive)
        {
            throw new InvalidOperationException($"Party '{partyCode}' was not found or is inactive.");
        }
    }
}

public sealed class ListWorkforceMembersUseCase : IListWorkforceMembersUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListWorkforceMembersUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<WorkforceMemberListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListWorkforceMembersAsync(cancellationToken);
    }
}

public sealed class TerminateWorkforceMemberUseCase : ITerminateWorkforceMemberUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public TerminateWorkforceMemberUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(string workforceMemberCode, CancellationToken cancellationToken)
    {
        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        member.Terminate();
        await _workforce.UpdateWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }
}

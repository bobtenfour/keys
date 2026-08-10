using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IRegisterWorkforceMemberUseCase
{
    /// <summary>
    /// Atomically creates Party identity and Active WorkforceMember. Returns generated WorkforceMemberCode.
    /// </summary>
    Task<string> ExecuteAsync(
        string firstName,
        string lastName,
        string uin,
        string workforceType,
        string organizationCode,
        string departmentCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken);
}

public interface IRegisterBootstrapWorkforcePairUseCase
{
    /// <summary>
    /// Atomically creates the first two Party + WorkforceMember records with mutual ResponsibleManager links.
    /// </summary>
    Task ExecuteAsync(
        string firstFirstName,
        string firstLastName,
        string firstUin,
        string firstWorkforceType,
        string secondFirstName,
        string secondLastName,
        string secondUin,
        string secondWorkforceType,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken);
}

public sealed class RegisterWorkforceMemberUseCase : IRegisterWorkforceMemberUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public RegisterWorkforceMemberUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task<string> ExecuteAsync(
        string firstName,
        string lastName,
        string uin,
        string workforceType,
        string organizationCode,
        string departmentCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken)
    {
        if (await _workforce.CountWorkforceMembersAsync(cancellationToken).ConfigureAwait(false) == 0)
        {
            throw new InvalidOperationException(
                "Create the initial workforce member pair before registering additional members.");
        }

        WorkforceMember? manager = await _workforce
            .FindWorkforceMemberAsync(responsibleManagerWorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (manager is null || manager.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("ResponsibleManager must be an existing Active WorkforceMember.");
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

        string partyCode = WorkforceIdentityCodes.NewPartyCode();
        string workforceMemberCode = WorkforceIdentityCodes.NewWorkforceMemberCode();

        Party party = new(partyCode, firstName, lastName, uin);
        if (await _workforce.PartyUinExistsAsync(party.Uin, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A party with this UIN already exists.");
        }

        WorkforceMember member = new(
            workforceMemberCode,
            party.PartyCode,
            CreateWorkforceMemberUseCase.ParseWorkforceType(workforceType),
            organization.OrganizationCode,
            department.DepartmentCode,
            manager.WorkforceMemberCode);

        await _workforce.AddPartyAndWorkforceMemberAsync(party, member, cancellationToken).ConfigureAwait(false);
        return workforceMemberCode;
    }
}

public sealed class RegisterBootstrapWorkforcePairUseCase : IRegisterBootstrapWorkforcePairUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public RegisterBootstrapWorkforcePairUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(
        string firstFirstName,
        string firstLastName,
        string firstUin,
        string firstWorkforceType,
        string secondFirstName,
        string secondLastName,
        string secondUin,
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

        string firstPartyCode = WorkforceIdentityCodes.NewPartyCode();
        string secondPartyCode = WorkforceIdentityCodes.NewPartyCode();
        string firstMemberCode = WorkforceIdentityCodes.NewWorkforceMemberCode();
        string secondMemberCode = WorkforceIdentityCodes.NewWorkforceMemberCode();

        Party firstParty = new(firstPartyCode, firstFirstName, firstLastName, firstUin);
        Party secondParty = new(secondPartyCode, secondFirstName, secondLastName, secondUin);

        if (string.Equals(firstParty.Uin, secondParty.Uin, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bootstrap workforce pair requires two different UIN values.");
        }

        if (await _workforce.PartyUinExistsAsync(firstParty.Uin, cancellationToken).ConfigureAwait(false)
            || await _workforce.PartyUinExistsAsync(secondParty.Uin, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A party with this UIN already exists.");
        }

        WorkforceMember first = new(
            firstMemberCode,
            firstParty.PartyCode,
            CreateWorkforceMemberUseCase.ParseWorkforceType(firstWorkforceType),
            organization.OrganizationCode,
            department.DepartmentCode,
            secondMemberCode);
        WorkforceMember second = new(
            secondMemberCode,
            secondParty.PartyCode,
            CreateWorkforceMemberUseCase.ParseWorkforceType(secondWorkforceType),
            organization.OrganizationCode,
            department.DepartmentCode,
            firstMemberCode);

        await _workforce
            .AddBootstrapPartiesAndWorkforceMembersAsync(firstParty, secondParty, first, second, cancellationToken)
            .ConfigureAwait(false);
    }
}

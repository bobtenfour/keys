using KeyInventory.Application.OperatorAudit;
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
        string departmentCode,
        CancellationToken cancellationToken);
}

public sealed class RegisterWorkforceMemberUseCase : IRegisterWorkforceMemberUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public RegisterWorkforceMemberUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<string> ExecuteAsync(
        string firstName,
        string lastName,
        string uin,
        string workforceType,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        Department? department = await _workforce.FindDepartmentAsync(departmentCode, cancellationToken)
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
            department.DepartmentCode);

        _audit.Stage(
            OperatorAuditActions.WorkforceMemberCreated,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"FirstName={party.FirstName}; LastName={party.LastName}; UIN={party.Uin}");
        await _workforce.AddPartyAndWorkforceMemberAsync(party, member, cancellationToken).ConfigureAwait(false);
        return workforceMemberCode;
    }
}

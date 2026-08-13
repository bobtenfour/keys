using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateWorkforceMemberUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string partyCode,
        string workforceType,
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
    private readonly IOperatorAuditRecorder _audit;

    public CreateWorkforceMemberUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string partyCode,
        string workforceType,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        (Party party, Department department) = await EnsureMemberPrerequisitesAsync(
                workforceMemberCode,
                partyCode,
                departmentCode,
                cancellationToken)
            .ConfigureAwait(false);

        WorkforceMember member = new(
            workforceMemberCode,
            partyCode,
            ParseWorkforceType(workforceType),
            department.DepartmentId);

        _audit.Stage(
            OperatorAuditActions.WorkforceMemberCreated,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"FirstName={party.FirstName}; LastName={party.LastName}; UIN={party.Uin}");
        await _workforce.AddWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(Party Party, Department Department)> EnsureMemberPrerequisitesAsync(
        string workforceMemberCode,
        string partyCode,
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

        Department? department = await _workforce.FindDepartmentByCodeAsync(departmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("The department was not found or is inactive.");
        }

        return (party, department);
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
    private readonly IOperatorAuditRecorder _audit;

    public TerminateWorkforceMemberUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
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
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberTerminated,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode);
        await _workforce.UpdateWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }
}

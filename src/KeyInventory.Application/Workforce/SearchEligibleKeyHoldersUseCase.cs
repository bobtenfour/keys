using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public sealed record EligibleKeyHolderCandidate(
    string WorkforceMemberCode,
    string FirstName,
    string LastName,
    string Uin);

public sealed record KeyHolderJustificationOption(string Code, string Label);

public sealed record KeyHolderIssueOptions(
    EligibleKeyHolderCandidate Holder,
    IReadOnlyList<KeyHolderJustificationOption> Departments,
    IReadOnlyList<KeyHolderJustificationOption> Rooms);

public interface ISearchEligibleKeyHoldersUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<EligibleKeyHolderCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public interface IGetKeyHolderIssueOptionsUseCase
{
    Task<KeyHolderIssueOptions?> ExecuteAsync(string workforceMemberCode, CancellationToken cancellationToken);
}

public sealed class SearchEligibleKeyHoldersUseCase : ISearchEligibleKeyHoldersUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public SearchEligibleKeyHoldersUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task<IReadOnlyList<EligibleKeyHolderCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        int bound = maxResults < 1
            ? ISearchEligibleKeyHoldersUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchEligibleKeyHoldersUseCase.DefaultMaxResults);

        IReadOnlyList<EligibleKeyHolderCandidate> matches = await _workforce
            .SearchEligibleKeyHoldersAsync(searchText.Trim(), bound, cancellationToken)
            .ConfigureAwait(false);

        List<EligibleKeyHolderCandidate> eligible = [];
        foreach (EligibleKeyHolderCandidate candidate in matches)
        {
            if (await IsEligibleCandidateAsync(candidate.WorkforceMemberCode, cancellationToken).ConfigureAwait(false))
            {
                eligible.Add(candidate);
            }

            if (eligible.Count >= bound)
            {
                break;
            }
        }

        return eligible;
    }

    private async Task<bool> IsEligibleCandidateAsync(string workforceMemberCode, CancellationToken cancellationToken)
    {
        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            return false;
        }

        Party? party = await _workforce.FindPartyAsync(member.PartyCode, cancellationToken).ConfigureAwait(false);
        Department? department = await _workforce.FindDepartmentAsync(member.DepartmentId, cancellationToken)
            .ConfigureAwait(false);
        if (party is null || department is null)
        {
            return false;
        }

        IReadOnlyList<WorkAssignment> assignments = await _workforce
            .ListActiveWorkAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            KeyIssueEligibility.EnsureIssueCandidate(member, party, department, assignments);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

public sealed class GetKeyHolderIssueOptionsUseCase : IGetKeyHolderIssueOptionsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public GetKeyHolderIssueOptionsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task<KeyHolderIssueOptions?> ExecuteAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workforceMemberCode))
        {
            return null;
        }

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            return null;
        }

        Party? party = await _workforce.FindPartyAsync(member.PartyCode, cancellationToken).ConfigureAwait(false);
        Department? department = await _workforce.FindDepartmentAsync(member.DepartmentId, cancellationToken)
            .ConfigureAwait(false);
        if (party is null || department is null)
        {
            return null;
        }

        IReadOnlyList<WorkAssignment> assignments = await _workforce
            .ListActiveWorkAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            KeyIssueEligibility.EnsureIssueCandidate(member, party, department, assignments);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        IReadOnlyList<RoomListItem> rooms = await _workforce.ListRoomsAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, RoomListItem> roomsByCode = rooms
            .Where(room => room.IsActive)
            .ToDictionary(room => room.RoomCode, StringComparer.OrdinalIgnoreCase);

        List<KeyHolderJustificationOption> roomOptions = assignments
            .Where(item => item.IsActive)
            .Select(item =>
            {
                if (roomsByCode.TryGetValue(item.RoomCode, out RoomListItem? room))
                {
                    return new KeyHolderJustificationOption(
                        item.RoomCode,
                        string.IsNullOrWhiteSpace(room.Description)
                            ? room.RoomNumber
                            : $"{room.RoomNumber} — {room.Description}");
                }

                return new KeyHolderJustificationOption(item.RoomCode, item.RoomCode);
            })
            .GroupBy(option => option.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new KeyHolderIssueOptions(
            new EligibleKeyHolderCandidate(
                member.WorkforceMemberCode,
                party.FirstName,
                party.LastName,
                party.Uin),
            [new KeyHolderJustificationOption(department.DepartmentCode, department.DepartmentCode)],
            roomOptions);
    }
}

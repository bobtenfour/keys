using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Readiness;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Operations;

public sealed class IssueModel : PageModel
{
    private const string SuccessTempDataKey = "IssueSuccessMessage";

    private readonly IIssueLoanUseCase _issueLoan;
    private readonly ISearchAvailableKeyCopiesUseCase _searchCopies;
    private readonly ISearchIssuablePhysicalCopiesUseCase _searchIssuable;
    private readonly ISearchEligibleKeyHoldersUseCase _searchHolders;
    private readonly IGetKeyHolderIssueOptionsUseCase _holderOptions;
    private readonly IOperationalReadinessUseCase _readiness;

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        ISearchAvailableKeyCopiesUseCase searchCopies,
        ISearchIssuablePhysicalCopiesUseCase searchIssuable,
        ISearchEligibleKeyHoldersUseCase searchHolders,
        IGetKeyHolderIssueOptionsUseCase holderOptions,
        IOperationalReadinessUseCase readiness)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _searchCopies = searchCopies ?? throw new ArgumentNullException(nameof(searchCopies));
        _searchIssuable = searchIssuable ?? throw new ArgumentNullException(nameof(searchIssuable));
        _searchHolders = searchHolders ?? throw new ArgumentNullException(nameof(searchHolders));
        _holderOptions = holderOptions ?? throw new ArgumentNullException(nameof(holderOptions));
        _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    [BindProperty]
    public string LoanCode { get; set; } = string.Empty;

    [BindProperty]
    public string KeyNumber { get; set; } = string.Empty;

    [BindProperty]
    public string MedecoKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string JustificationKind { get; set; } = string.Empty;

    [BindProperty]
    public string JustificationCode { get; set; } = string.Empty;

    [BindProperty]
    public string IssuedLocalText { get; set; } = string.Empty;

    [BindProperty]
    public string DueLocalText { get; set; } = string.Empty;

    public string ClassificationDisplay { get; private set; } = string.Empty;

    public string OpenedRoomsDisplay { get; private set; } = "—";

    public string? SelectedHolderDisplay { get; private set; }

    public string SelectedKeyDisplay { get; private set; } = string.Empty;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public bool HasAvailableCopies { get; private set; }

    public OperationalReadinessViewModel Readiness { get; private set; } = null!;

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue(SuccessTempDataKey, out object? success) && success is string text)
        {
            SuccessMessage = text;
        }

        await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
        IssuedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
        DueLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow.AddDays(1));
    }

    /// <summary>
    /// JSON handler used by the searchable combobox to browse/search issuable physical copies.
    /// </summary>
    public async Task<IActionResult> OnGetSearchIssuableCopiesAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<IssuablePhysicalCopyItem> matches = await _searchIssuable
            .ExecuteAsync(q ?? string.Empty, ISearchIssuablePhysicalCopiesUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                keyNumber = item.KeyNumber,
                medecoKeyCode = item.MedecoKeyCode,
                classification = item.Classification.ToString(),
                rooms = KeyOpenedRoomDisplayFormatter.FormatAccess(item.Classification, item.OpenedRooms)
            })
            .ToArray();
        return new JsonResult(result);
    }

    /// <summary>
    /// JSON handler used by the searchable combobox to browse/search eligible key holders.
    /// </summary>
    public async Task<IActionResult> OnGetSearchHoldersAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<EligibleKeyHolderCandidate> matches = await _searchHolders
            .ExecuteAsync(q ?? string.Empty, ISearchEligibleKeyHoldersUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(candidate => new
            {
                workforceMemberCode = candidate.WorkforceMemberCode,
                display = PartyHolderDisplayFormatter.Format(candidate.FirstName, candidate.LastName, candidate.Uin),
                uin = candidate.Uin,
                firstName = candidate.FirstName,
                lastName = candidate.LastName
            })
            .ToArray();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
        await LoadSelectedHolderAsync(cancellationToken).ConfigureAwait(false);
        await LoadSelectedKeyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (string.IsNullOrWhiteSpace(WorkforceMemberCode))
            {
                throw new InvalidOperationException("Select a key holder.");
            }

            if (string.IsNullOrWhiteSpace(KeyNumber) || string.IsNullOrWhiteSpace(MedecoKeyCode))
            {
                throw new InvalidOperationException("Select an issuable KEY # / MEDECO.");
            }

            if (string.IsNullOrWhiteSpace(JustificationKind))
            {
                throw new InvalidOperationException("Select whether the issue is for a Department or a Room.");
            }

            if (string.IsNullOrWhiteSpace(JustificationCode))
            {
                throw new InvalidOperationException("Select the justification Department or Room.");
            }

            if (!OperatorLocalTimestamp.TryParseToUtc(IssuedLocalText, out DateTimeOffset issuedAtUtc, out string? issuedError))
            {
                throw new InvalidOperationException(issuedError ?? "Issued time is invalid.");
            }

            if (!OperatorLocalTimestamp.TryParseToUtc(DueLocalText, out DateTimeOffset dueAtUtc, out string? dueError))
            {
                throw new InvalidOperationException(dueError ?? "Due time is invalid.");
            }

            await _issueLoan.ExecuteAsync(
                    LoanCode,
                    KeyNumber,
                    MedecoKeyCode,
                    WorkforceMemberCode,
                    JustificationKind,
                    JustificationCode,
                    issuedAtUtc,
                    dueAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            TempData[SuccessTempDataKey] =
                $"{PartyHolderDisplayFormatter.FormatKeyCopy(KeyNumber, MedecoKeyCode)} was issued to {SelectedHolderDisplay}.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    private async Task LoadReadinessAsync(CancellationToken cancellationToken)
    {
        OperationalReadinessSnapshot snapshot = await _readiness.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Readiness = new OperationalReadinessViewModel(snapshot);
        HasAvailableCopies = await _searchCopies.HasAnyAvailableAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadSelectedKeyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KeyNumber) || string.IsNullOrWhiteSpace(MedecoKeyCode))
        {
            OpenedRoomsDisplay = "—";
            ClassificationDisplay = string.Empty;
            SelectedKeyDisplay = string.Empty;
            return;
        }

        AvailableKeyCopyCandidate? candidate = await _searchCopies
            .FindAsync(KeyNumber, MedecoKeyCode, cancellationToken)
            .ConfigureAwait(false);
        if (candidate is null)
        {
            OpenedRoomsDisplay = "—";
            ClassificationDisplay = string.Empty;
            SelectedKeyDisplay = string.Empty;
            KeyNumber = string.Empty;
            MedecoKeyCode = string.Empty;
            return;
        }

        ClassificationDisplay = candidate.Classification.ToString();
        string rooms = KeyOpenedRoomDisplayFormatter.FormatAccess(candidate.Classification, candidate.OpenedRooms);
        OpenedRoomsDisplay = string.IsNullOrEmpty(rooms) ? "—" : rooms;
        SelectedKeyDisplay = PartyHolderDisplayFormatter.FormatKeyCopy(candidate.KeyNumber, candidate.MedecoKeyCode);
    }

    private async Task LoadSelectedHolderAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(WorkforceMemberCode))
        {
            SelectedHolderDisplay = null;
            DepartmentOptions = [];
            RoomOptions = [];
            return;
        }

        KeyHolderIssueOptions? options = await _holderOptions
            .ExecuteAsync(WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (options is null)
        {
            SelectedHolderDisplay = null;
            DepartmentOptions = [];
            RoomOptions = [];
            ErrorMessage ??= "The selected key holder is not eligible to receive a key.";
            WorkforceMemberCode = string.Empty;
            return;
        }

        SelectedHolderDisplay = PartyHolderDisplayFormatter.Format(
            options.Holder.FirstName,
            options.Holder.LastName,
            options.Holder.Uin);

        DepartmentOptions = options.Departments
            .Select(choice => new SelectListItem(
                choice.Label,
                choice.Code,
                string.Equals(choice.Code, JustificationCode, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(JustificationKind, "Department", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        RoomOptions = options.Rooms
            .Select(choice => new SelectListItem(
                choice.Label,
                choice.Code,
                string.Equals(choice.Code, JustificationCode, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(JustificationKind, "Room", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}

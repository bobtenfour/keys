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
    private const string SelectedHolderTempDataKey = "IssueSelectedHolderCode";
    private const string SelectedKeyNumberTempDataKey = "IssueSelectedKeyNumber";
    private const string SelectedMedecoTempDataKey = "IssueSelectedMedeco";

    private readonly IIssueLoanUseCase _issueLoan;
    private readonly ISearchAvailableKeyCopiesUseCase _searchCopies;
    private readonly ISearchEligibleKeyHoldersUseCase _searchHolders;
    private readonly IGetKeyHolderIssueOptionsUseCase _holderOptions;
    private readonly IOperationalReadinessUseCase _readiness;

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        ISearchAvailableKeyCopiesUseCase searchCopies,
        ISearchEligibleKeyHoldersUseCase searchHolders,
        IGetKeyHolderIssueOptionsUseCase holderOptions,
        IOperationalReadinessUseCase readiness)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _searchCopies = searchCopies ?? throw new ArgumentNullException(nameof(searchCopies));
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

    [BindProperty]
    public string HolderSearchText { get; set; } = string.Empty;

    [BindProperty]
    public string KeyCopySearchText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> MedecoOptions { get; private set; } = [];

    public string OpenedRoomsDisplay { get; private set; } = "—";

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public IReadOnlyList<EligibleKeyHolderCandidate> HolderMatches { get; private set; } = [];

    public IReadOnlyList<AvailableKeyCopyCandidate> KeyCopyMatches { get; private set; } = [];

    public string? SelectedHolderDisplay { get; private set; }

    public bool HolderSearchPerformed { get; private set; }

    public bool KeyCopySearchPerformed { get; private set; }

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

        ResetCleanBusinessChoices();
        await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
        IssuedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
        DueLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow.AddDays(1));

        if (TempData.Peek(SelectedHolderTempDataKey) is string selectedCode
            && !string.IsNullOrWhiteSpace(selectedCode)
            && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            WorkforceMemberCode = selectedCode;
            await LoadSelectedHolderAsync(cancellationToken).ConfigureAwait(false);
        }

        if (TempData.Peek(SelectedKeyNumberTempDataKey) is string selectedKey
            && !string.IsNullOrWhiteSpace(selectedKey)
            && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            KeyNumber = selectedKey;
            await LoadSelectedKeyAsync(cancellationToken).ConfigureAwait(false);
            if (TempData.Peek(SelectedMedecoTempDataKey) is string selectedMedeco
                && !string.IsNullOrWhiteSpace(selectedMedeco))
            {
                MedecoKeyCode = selectedMedeco;
            }
        }
    }

    public async Task<IActionResult> OnPostSearchHoldersAsync(CancellationToken cancellationToken)
    {
        TempData.Remove(SelectedHolderTempDataKey);
        await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
        EnsureDefaultTimestamps();
        HolderSearchPerformed = true;
        WorkforceMemberCode = string.Empty;
        SelectedHolderDisplay = null;
        JustificationKind = string.Empty;
        JustificationCode = string.Empty;
        DepartmentOptions = [];
        RoomOptions = [];
        await RestoreSelectedKeyFromTempDataAsync(cancellationToken).ConfigureAwait(false);

        HolderMatches = await _searchHolders
            .ExecuteAsync(HolderSearchText, ISearchEligibleKeyHoldersUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        return Page();
    }

    public async Task<IActionResult> OnPostSelectHolderAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(WorkforceMemberCode))
        {
            ErrorMessage = "Select a key holder.";
            await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
            EnsureDefaultTimestamps();
            return Page();
        }

        KeyHolderIssueOptions? options = await _holderOptions
            .ExecuteAsync(WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (options is null)
        {
            ErrorMessage = "The selected key holder is not eligible to receive a key.";
            await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
            EnsureDefaultTimestamps();
            return Page();
        }

        TempData[SelectedHolderTempDataKey] = WorkforceMemberCode;
        return RedirectToPage();
    }

    public IActionResult OnPostClearHolder()
    {
        TempData.Remove(SelectedHolderTempDataKey);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSearchKeyCopiesAsync(CancellationToken cancellationToken)
    {
        TempData.Remove(SelectedKeyNumberTempDataKey);
        TempData.Remove(SelectedMedecoTempDataKey);
        await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
        EnsureDefaultTimestamps();
        await RestoreSelectedHolderFromTempDataAsync(cancellationToken).ConfigureAwait(false);
        KeyCopySearchPerformed = true;
        KeyNumber = string.Empty;
        MedecoKeyCode = string.Empty;
        OpenedRoomsDisplay = "—";
        MedecoOptions = [];
        KeyCopyMatches = await _searchCopies
            .ExecuteAsync(KeyCopySearchText, ISearchAvailableKeyCopiesUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostSelectKeyNumberAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KeyNumber))
        {
            ErrorMessage = "Select a KEY #.";
            await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
            EnsureDefaultTimestamps();
            await RestoreSelectedHolderFromTempDataAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }

        IReadOnlyList<AvailableKeyCopyCandidate> copies = await _searchCopies
            .ListAvailableForKeyNumberAsync(KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (copies.Count == 0)
        {
            ErrorMessage = "No available MEDECO copies were found for that KEY #.";
            await LoadReadinessAsync(cancellationToken).ConfigureAwait(false);
            EnsureDefaultTimestamps();
            await RestoreSelectedHolderFromTempDataAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }

        TempData[SelectedKeyNumberTempDataKey] = copies[0].KeyNumber;
        TempData.Remove(SelectedMedecoTempDataKey);
        return RedirectToPage();
    }

    public IActionResult OnPostClearKeyNumber()
    {
        TempData.Remove(SelectedKeyNumberTempDataKey);
        TempData.Remove(SelectedMedecoTempDataKey);
        return RedirectToPage();
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
                throw new InvalidOperationException("Select KEY # and an available MEDECO Key Code.");
            }

            if (string.IsNullOrWhiteSpace(JustificationKind))
            {
                throw new InvalidOperationException("Select whether the issue is for a Department or a Room.");
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

            TempData.Remove(SelectedHolderTempDataKey);
            TempData.Remove(SelectedKeyNumberTempDataKey);
            TempData.Remove(SelectedMedecoTempDataKey);
            TempData[SuccessTempDataKey] =
                $"{PartyHolderDisplayFormatter.FormatKeyCopy(KeyNumber, MedecoKeyCode)} was issued to {SelectedHolderDisplay}.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            if (!string.IsNullOrWhiteSpace(WorkforceMemberCode))
            {
                TempData[SelectedHolderTempDataKey] = WorkforceMemberCode;
            }

            if (!string.IsNullOrWhiteSpace(KeyNumber))
            {
                TempData[SelectedKeyNumberTempDataKey] = KeyNumber;
            }

            if (!string.IsNullOrWhiteSpace(MedecoKeyCode))
            {
                TempData[SelectedMedecoTempDataKey] = MedecoKeyCode;
            }

            return Page();
        }
    }

    private void ResetCleanBusinessChoices()
    {
        LoanCode = string.Empty;
        KeyNumber = string.Empty;
        MedecoKeyCode = string.Empty;
        WorkforceMemberCode = string.Empty;
        JustificationKind = string.Empty;
        JustificationCode = string.Empty;
        HolderSearchText = string.Empty;
        KeyCopySearchText = string.Empty;
        HolderMatches = [];
        KeyCopyMatches = [];
        HolderSearchPerformed = false;
        KeyCopySearchPerformed = false;
        SelectedHolderDisplay = null;
        DepartmentOptions = [];
        RoomOptions = [];
        OpenedRoomsDisplay = "—";
        MedecoOptions = [];
    }

    private void EnsureDefaultTimestamps()
    {
        if (string.IsNullOrWhiteSpace(IssuedLocalText))
        {
            IssuedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
        }

        if (string.IsNullOrWhiteSpace(DueLocalText))
        {
            DueLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow.AddDays(1));
        }
    }

    private async Task LoadReadinessAsync(CancellationToken cancellationToken)
    {
        OperationalReadinessSnapshot snapshot = await _readiness.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Readiness = new OperationalReadinessViewModel(snapshot);
        HasAvailableCopies = await _searchCopies.HasAnyAvailableAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RestoreSelectedHolderFromTempDataAsync(CancellationToken cancellationToken)
    {
        if (TempData.Peek(SelectedHolderTempDataKey) is string selectedCode
            && !string.IsNullOrWhiteSpace(selectedCode))
        {
            WorkforceMemberCode = selectedCode;
            await LoadSelectedHolderAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RestoreSelectedKeyFromTempDataAsync(CancellationToken cancellationToken)
    {
        if (TempData.Peek(SelectedKeyNumberTempDataKey) is string selectedKey
            && !string.IsNullOrWhiteSpace(selectedKey))
        {
            KeyNumber = selectedKey;
            await LoadSelectedKeyAsync(cancellationToken).ConfigureAwait(false);
            if (TempData.Peek(SelectedMedecoTempDataKey) is string selectedMedeco
                && !string.IsNullOrWhiteSpace(selectedMedeco))
            {
                MedecoKeyCode = selectedMedeco;
            }
        }
    }

    private async Task LoadSelectedKeyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KeyNumber))
        {
            OpenedRoomsDisplay = "—";
            MedecoOptions = [];
            return;
        }

        IReadOnlyList<AvailableKeyCopyCandidate> copies = await _searchCopies
            .ListAvailableForKeyNumberAsync(KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (copies.Count == 0)
        {
            OpenedRoomsDisplay = "—";
            MedecoOptions = [];
            KeyNumber = string.Empty;
            MedecoKeyCode = string.Empty;
            TempData.Remove(SelectedKeyNumberTempDataKey);
            TempData.Remove(SelectedMedecoTempDataKey);
            return;
        }

        KeyNumber = copies[0].KeyNumber;
        string rooms = KeyOpenedRoomDisplayFormatter.Format(copies[0].OpenedRooms);
        OpenedRoomsDisplay = string.IsNullOrEmpty(rooms) ? "—" : rooms;
        MedecoOptions = copies
            .Select(copy => new SelectListItem(
                copy.MedecoKeyCode,
                copy.MedecoKeyCode,
                string.Equals(copy.MedecoKeyCode, MedecoKeyCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        // Do not auto-select first/only MEDECO.
        if (!MedecoOptions.Any(item => item.Selected))
        {
            MedecoKeyCode = string.Empty;
        }
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

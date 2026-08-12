using System.Text.Json;
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

    private static readonly JsonSerializerOptions KeyCopyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IIssueLoanUseCase _issueLoan;
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IListOpenLoansUseCase _listOpenLoans;
    private readonly ISearchEligibleKeyHoldersUseCase _searchHolders;
    private readonly IGetKeyHolderIssueOptionsUseCase _holderOptions;
    private readonly IOperationalReadinessUseCase _readiness;

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        IListKeyAssetsUseCase listKeyAssets,
        IListOpenLoansUseCase listOpenLoans,
        ISearchEligibleKeyHoldersUseCase searchHolders,
        IGetKeyHolderIssueOptionsUseCase holderOptions,
        IOperationalReadinessUseCase readiness)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
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

    public IReadOnlyList<SelectListItem> KeyNumberOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> MedecoOptions { get; private set; } = [];

    public string OpenedRoomsDisplay { get; private set; } = "—";

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public IReadOnlyList<EligibleKeyHolderCandidate> HolderMatches { get; private set; } = [];

    public string? SelectedHolderDisplay { get; private set; }

    public bool HolderSearchPerformed { get; private set; }

    public string KeyCopyDataJson { get; private set; } = "{}";

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
        await LoadCatalogAndReadinessAsync(cancellationToken).ConfigureAwait(false);
        IssuedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
        DueLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow.AddDays(1));

        if (TempData.Peek(SelectedHolderTempDataKey) is string selectedCode
            && !string.IsNullOrWhiteSpace(selectedCode)
            && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            WorkforceMemberCode = selectedCode;
            await LoadSelectedHolderAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IActionResult> OnPostSearchHoldersAsync(CancellationToken cancellationToken)
    {
        TempData.Remove(SelectedHolderTempDataKey);
        await LoadCatalogAndReadinessAsync(cancellationToken).ConfigureAwait(false);
        EnsureDefaultTimestamps();
        HolderSearchPerformed = true;
        WorkforceMemberCode = string.Empty;
        SelectedHolderDisplay = null;
        JustificationKind = string.Empty;
        JustificationCode = string.Empty;
        DepartmentOptions = [];
        RoomOptions = [];

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
            await LoadCatalogAndReadinessAsync(cancellationToken).ConfigureAwait(false);
            EnsureDefaultTimestamps();
            return Page();
        }

        KeyHolderIssueOptions? options = await _holderOptions
            .ExecuteAsync(WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (options is null)
        {
            ErrorMessage = "The selected key holder is not eligible to receive a key.";
            await LoadCatalogAndReadinessAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadCatalogAndReadinessAsync(cancellationToken).ConfigureAwait(false);
        await LoadSelectedHolderAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (string.IsNullOrWhiteSpace(WorkforceMemberCode))
            {
                throw new InvalidOperationException("Select a key holder.");
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
        HolderMatches = [];
        HolderSearchPerformed = false;
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

    private async Task LoadCatalogAndReadinessAsync(CancellationToken cancellationToken)
    {
        OperationalReadinessSnapshot snapshot = await _readiness.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Readiness = new OperationalReadinessViewModel(snapshot);

        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        HashSet<Guid> issued = openItems.Select(item => item.KeyAssetId).ToHashSet();

        List<KeyAssetListItem> available = keys
            .Where(key => key.IsActive && !issued.Contains(key.KeyAssetId))
            .OrderBy(key => key.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(key => key.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        HasAvailableCopies = available.Count > 0;

        Dictionary<string, KeyNumberIssueOptions> byKeyNumber = available
            .GroupBy(key => key.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    string rooms = KeyOpenedRoomDisplayFormatter.Format(group.First().OpenedRooms);
                    return new KeyNumberIssueOptions(
                        string.IsNullOrEmpty(rooms) ? "—" : rooms,
                        group.Select(copy => new MedecoChoice(copy.MedecoKeyCode, copy.MedecoKeyCode)).ToArray());
                },
                StringComparer.OrdinalIgnoreCase);

        KeyCopyDataJson = JsonSerializer.Serialize(byKeyNumber, KeyCopyJsonOptions);

        KeyNumberOptions = byKeyNumber.Keys
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Select(key => new SelectListItem(
                key,
                key,
                string.Equals(key, KeyNumber, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(KeyNumber)
            && byKeyNumber.TryGetValue(KeyNumber, out KeyNumberIssueOptions? selectedKey))
        {
            OpenedRoomsDisplay = selectedKey.Rooms;
            MedecoOptions = selectedKey.Medecos
                .Select(choice => new SelectListItem(
                    choice.Code,
                    choice.Code,
                    string.Equals(choice.Code, MedecoKeyCode, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
        else
        {
            OpenedRoomsDisplay = "—";
            MedecoOptions = [];
            if (!string.IsNullOrWhiteSpace(KeyNumber))
            {
                // Keep operator-entered KEY # for validation retention, but clear MEDECO options.
                MedecoKeyCode = string.IsNullOrWhiteSpace(MedecoKeyCode) ? string.Empty : MedecoKeyCode;
            }
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

    private sealed record MedecoChoice(string Code, string Label);

    private sealed record KeyNumberIssueOptions(
        string Rooms,
        IReadOnlyList<MedecoChoice> Medecos);
}

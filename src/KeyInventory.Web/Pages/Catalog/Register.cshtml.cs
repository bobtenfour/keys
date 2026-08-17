using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class RegisterModel : PageModel
{
    private const string SuccessTempDataKey = "RegisterSuccessMessage";

    public const string ModeNew = "New";
    public const string ModeReplace = "Replace";

    private readonly ICreateKeyAssetUseCase _createKeyAsset;
    private readonly IReplaceLostKeyUseCase _replaceLostKey;
    private readonly IGetKeyNumberRegistrationPreviewUseCase _preview;
    private readonly ISearchKeyNumbersForRegistrationUseCase _searchKeyNumbers;
    private readonly ISearchActiveRoomsUseCase _searchRooms;
    private readonly ISearchLostKeysUseCase _searchLostKeys;

    public RegisterModel(
        ICreateKeyAssetUseCase createKeyAsset,
        IReplaceLostKeyUseCase replaceLostKey,
        IGetKeyNumberRegistrationPreviewUseCase preview,
        ISearchKeyNumbersForRegistrationUseCase searchKeyNumbers,
        ISearchActiveRoomsUseCase searchRooms,
        ISearchLostKeysUseCase searchLostKeys)
    {
        _createKeyAsset = createKeyAsset ?? throw new ArgumentNullException(nameof(createKeyAsset));
        _replaceLostKey = replaceLostKey ?? throw new ArgumentNullException(nameof(replaceLostKey));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        _searchKeyNumbers = searchKeyNumbers ?? throw new ArgumentNullException(nameof(searchKeyNumbers));
        _searchRooms = searchRooms ?? throw new ArgumentNullException(nameof(searchRooms));
        _searchLostKeys = searchLostKeys ?? throw new ArgumentNullException(nameof(searchLostKeys));
    }

    [BindProperty]
    public string Mode { get; set; } = ModeNew;

    [BindProperty]
    public string KeyNumber { get; set; } = string.Empty;

    [BindProperty]
    public string MedecoKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string Classification { get; set; } = string.Empty;

    [BindProperty]
    public string SelectedRoomCodes { get; set; } = string.Empty;

    [BindProperty]
    public Guid? LostKeyAssetId { get; set; }

    public KeyNumberRegistrationPreview? SelectedKeyPreview { get; private set; }

    public LostKeyCandidate? SelectedLostKey { get; private set; }

    /// <summary>
    /// True when New Key has resolved an existing KEY # through Application authority.
    /// </summary>
    public bool KeyNumberExists => SelectedKeyPreview is not null;

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string ModeHeading => Mode == ModeReplace
        ? "Replace a Lost key with a new MEDECO"
        : "Create a key";

    public string ModeHelp => Mode == ModeReplace
        ? "Select a Lost key, then enter a new MEDECO. The replacement stays under the same KEY # and keeps the same access."
        : "Enter a KEY # and MEDECO. If the KEY # already exists, Classification and Access are shown as information. If it does not, choose Classification and (for Regular) exactly one Room.";

    public string SubmitLabel => Mode == ModeReplace ? "Replace Key" : "Create Key";

    public async Task OnGetAsync(string? mode, CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue(SuccessTempDataKey, out object? success) && success is string text)
        {
            SuccessMessage = text;
            Mode = ModeNew;
            ClearOperatorInputs();
            return;
        }

        Mode = NormalizeMode(mode);
        await RestorePreviewAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetSearchKeyNumbersAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyNumberRegistrationPreview> matches = await _searchKeyNumbers
            .ExecuteAsync(q ?? string.Empty, ISearchKeyNumbersForRegistrationUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Where(item => item.IsActive)
            .Select(item => new
            {
                keyNumber = item.KeyNumber,
                classification = item.Classification.ToString(),
                access = KeyOpenedRoomDisplayFormatter.FormatAccess(item.Classification, item.OpenedRooms)
            })
            .ToArray();

        return new JsonResult(result);
    }

    /// <summary>
    /// Resolves whether a typed KEY # already exists under Application authority.
    /// </summary>
    public async Task<IActionResult> OnGetResolveKeyNumberAsync(string? keyNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyNumber))
        {
            return new JsonResult(new { exists = false });
        }

        KeyNumberRegistrationPreview? preview = await _preview
            .ExecuteAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);

        if (preview is null || !preview.IsActive)
        {
            return new JsonResult(new { exists = false });
        }

        return new JsonResult(new
        {
            exists = true,
            keyNumber = preview.KeyNumber,
            classification = preview.Classification.ToString(),
            access = KeyOpenedRoomDisplayFormatter.FormatAccess(preview.Classification, preview.OpenedRooms)
        });
    }

    public async Task<IActionResult> OnGetSearchRoomsAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomListItem> matches = await _searchRooms
            .ExecuteAsync(q ?? string.Empty, ISearchActiveRoomsUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                roomCode = item.RoomCode,
                roomNumber = item.RoomNumber,
                description = item.Description ?? string.Empty,
                department = item.DepartmentCode
            })
            .ToArray();

        return new JsonResult(result);
    }

    public async Task<IActionResult> OnGetSearchLostKeysAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<LostKeyCandidate> matches = await _searchLostKeys
            .ExecuteAsync(q ?? string.Empty, ISearchLostKeysUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                keyAssetId = item.KeyAssetId.ToString("D"),
                keyNumber = item.KeyNumber,
                medeco = item.MedecoKeyCode,
                classification = item.Classification.ToString(),
                access = KeyOpenedRoomDisplayFormatter.FormatAccess(item.Classification, item.OpenedRooms),
                label = $"KEY # {item.KeyNumber} / MEDECO {item.MedecoKeyCode}"
            })
            .ToArray();

        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Mode = NormalizeMode(Mode);
        try
        {
            if (Mode == ModeReplace)
            {
                if (LostKeyAssetId is null || LostKeyAssetId == Guid.Empty)
                {
                    throw new InvalidOperationException("Select a Lost key to replace.");
                }

                if (string.IsNullOrWhiteSpace(MedecoKeyCode))
                {
                    throw new InvalidOperationException("Enter the new MEDECO.");
                }

                LostKeyCandidate? source = await FindLostKeyAsync(LostKeyAssetId.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (source is null)
                {
                    throw new InvalidOperationException("Select a Lost key to replace.");
                }

                await _replaceLostKey
                    .ExecuteAsync(LostKeyAssetId.Value, MedecoKeyCode, cancellationToken)
                    .ConfigureAwait(false);

                TempData[SuccessTempDataKey] =
                    $"KEY # {source.KeyNumber} / MEDECO {MedecoKeyCode.Trim()} was created as a replacement.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(KeyNumber))
                {
                    throw new InvalidOperationException("Enter a KEY #.");
                }

                if (string.IsNullOrWhiteSpace(MedecoKeyCode))
                {
                    throw new InvalidOperationException("Enter a MEDECO.");
                }

                KeyNumberRegistrationPreview? existing = await _preview
                    .ExecuteAsync(KeyNumber, cancellationToken)
                    .ConfigureAwait(false);

                KeyAccessClassification? classification = null;
                IReadOnlyList<string>? rooms = null;
                if (existing is null)
                {
                    if (!Enum.TryParse(Classification, ignoreCase: true, out KeyAccessClassification parsed)
                        || parsed is not (KeyAccessClassification.Regular or KeyAccessClassification.Master))
                    {
                        throw new InvalidOperationException("Select Regular or Master.");
                    }

                    classification = parsed;
                    rooms = ParseRoomCodes(SelectedRoomCodes).ToArray();
                }

                RegisterNewKeyResult result = await _createKeyAsset
                    .RegisterNewKeyAsync(KeyNumber, MedecoKeyCode, classification, rooms, cancellationToken)
                    .ConfigureAwait(false);

                TempData[SuccessTempDataKey] =
                    $"KEY # {result.KeyNumber} / MEDECO {result.MedecoKeyCode} was created.";
            }

            return RedirectToPage(new { mode = ModeNew });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            await RestorePreviewAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }
    }

    private async Task RestorePreviewAsync(CancellationToken cancellationToken)
    {
        if (Mode == ModeNew && !string.IsNullOrWhiteSpace(KeyNumber))
        {
            SelectedKeyPreview = await _preview.ExecuteAsync(KeyNumber, cancellationToken).ConfigureAwait(false);
            if (SelectedKeyPreview is not null)
            {
                // Existing KEY #: discard any unsaved Classification/Room input from a prior non-existing entry.
                Classification = string.Empty;
                SelectedRoomCodes = string.Empty;
            }
        }

        if (Mode == ModeReplace && LostKeyAssetId is Guid lostId && lostId != Guid.Empty)
        {
            SelectedLostKey = await FindLostKeyAsync(lostId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<LostKeyCandidate?> FindLostKeyAsync(Guid lostKeyAssetId, CancellationToken cancellationToken)
    {
        IReadOnlyList<LostKeyCandidate> lost = await _searchLostKeys
            .ExecuteAsync(string.Empty, ISearchLostKeysUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);
        return lost.FirstOrDefault(item => item.KeyAssetId == lostKeyAssetId);
    }

    private void ClearOperatorInputs()
    {
        KeyNumber = string.Empty;
        MedecoKeyCode = string.Empty;
        Classification = string.Empty;
        SelectedRoomCodes = string.Empty;
        LostKeyAssetId = null;
        SelectedKeyPreview = null;
        SelectedLostKey = null;
    }

    private static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, ModeReplace, StringComparison.OrdinalIgnoreCase))
        {
            return ModeReplace;
        }

        return ModeNew;
    }

    private static IEnumerable<string> ParseRoomCodes(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            yield break;
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (seen.Add(raw))
            {
                yield return raw;
            }
        }
    }
}

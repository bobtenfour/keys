using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class RegisterModel : PageModel
{
    private const string SuccessTempDataKey = "RegisterSuccessMessage";
    private const string SelectedKeyTempDataKey = "RegisterSelectedKeyNumber";
    private const string ModeTempDataKey = "RegisterMode";

    public const string ModeExisting = "Existing";
    public const string ModeNew = "New";

    private readonly ICreateKeyAssetUseCase _createKeyAsset;
    private readonly ISearchKeyNumbersForRegistrationUseCase _searchKeyNumbers;
    private readonly IGetKeyNumberRegistrationPreviewUseCase _preview;
    private readonly IListKeyTypesUseCase _listKeyTypes;

    public RegisterModel(
        ICreateKeyAssetUseCase createKeyAsset,
        ISearchKeyNumbersForRegistrationUseCase searchKeyNumbers,
        IGetKeyNumberRegistrationPreviewUseCase preview,
        IListKeyTypesUseCase listKeyTypes)
    {
        _createKeyAsset = createKeyAsset ?? throw new ArgumentNullException(nameof(createKeyAsset));
        _searchKeyNumbers = searchKeyNumbers ?? throw new ArgumentNullException(nameof(searchKeyNumbers));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        _listKeyTypes = listKeyTypes ?? throw new ArgumentNullException(nameof(listKeyTypes));
    }

    [BindProperty]
    public string Mode { get; set; } = ModeExisting;

    [BindProperty]
    public string KeyNumber { get; set; } = string.Empty;

    [BindProperty]
    public string MedecoKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string TypeCode { get; set; } = string.Empty;

    [BindProperty]
    public string KeyNumberSearchText { get; set; } = string.Empty;

    public IReadOnlyList<KeyNumberRegistrationPreview> KeyNumberMatches { get; private set; } = [];

    public bool KeyNumberSearchPerformed { get; private set; }

    public KeyNumberRegistrationPreview? SelectedKeyPreview { get; private set; }

    public IReadOnlyList<SelectListItem> KeyTypeOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue(SuccessTempDataKey, out object? success) && success is string text)
        {
            SuccessMessage = text;
        }

        Mode = TempData.Peek(ModeTempDataKey) as string ?? ModeExisting;
        if (TempData.Peek(SelectedKeyTempDataKey) is string selected
            && !string.IsNullOrWhiteSpace(selected)
            && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            KeyNumber = selected;
            SelectedKeyPreview = await _preview.ExecuteAsync(selected, cancellationToken).ConfigureAwait(false);
            Mode = ModeExisting;
        }

        await LoadKeyTypesAsync(cancellationToken).ConfigureAwait(false);
    }

    public IActionResult OnPostSetMode(string mode)
    {
        TempData.Remove(SelectedKeyTempDataKey);
        TempData[ModeTempDataKey] = string.Equals(mode, ModeNew, StringComparison.OrdinalIgnoreCase)
            ? ModeNew
            : ModeExisting;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSearchKeyNumbersAsync(CancellationToken cancellationToken)
    {
        TempData[ModeTempDataKey] = ModeExisting;
        TempData.Remove(SelectedKeyTempDataKey);
        Mode = ModeExisting;
        KeyNumberSearchPerformed = true;
        KeyNumber = string.Empty;
        SelectedKeyPreview = null;
        KeyNumberMatches = await _searchKeyNumbers
            .ExecuteAsync(KeyNumberSearchText, ISearchKeyNumbersForRegistrationUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);
        await LoadKeyTypesAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostSelectKeyNumberAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KeyNumber))
        {
            ErrorMessage = "Select an existing KEY #.";
            Mode = ModeExisting;
            await LoadKeyTypesAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }

        KeyNumberRegistrationPreview? preview = await _preview.ExecuteAsync(KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (preview is null || !preview.IsActive)
        {
            ErrorMessage = "The KEY # was not found or is inactive.";
            Mode = ModeExisting;
            await LoadKeyTypesAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }

        TempData[SelectedKeyTempDataKey] = preview.KeyNumber;
        TempData[ModeTempDataKey] = ModeExisting;
        return RedirectToPage();
    }

    public IActionResult OnPostClearKeyNumber()
    {
        TempData.Remove(SelectedKeyTempDataKey);
        TempData[ModeTempDataKey] = ModeExisting;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.Equals(Mode, ModeExisting, StringComparison.OrdinalIgnoreCase))
            {
                await _createKeyAsset
                    .RegisterPhysicalCopyUnderExistingKeyNumberAsync(KeyNumber, MedecoKeyCode, cancellationToken)
                    .ConfigureAwait(false);
                TempData[SuccessTempDataKey] =
                    $"Physical copy MEDECO {MedecoKeyCode.Trim()} was registered under KEY # {KeyNumber.Trim()}.";
            }
            else
            {
                await _createKeyAsset
                    .CreateNewKeyNumberWithFirstPhysicalCopyAsync(KeyNumber, TypeCode, MedecoKeyCode, cancellationToken)
                    .ConfigureAwait(false);
                TempData[SuccessTempDataKey] =
                    $"KEY # {KeyNumber.Trim()} was created with MEDECO {MedecoKeyCode.Trim()}. Assign Rooms opened on KEY # Rooms when needed.";
            }

            TempData.Remove(SelectedKeyTempDataKey);
            TempData.Remove(ModeTempDataKey);
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            if (string.Equals(Mode, ModeExisting, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(KeyNumber))
            {
                SelectedKeyPreview = await _preview.ExecuteAsync(KeyNumber, cancellationToken).ConfigureAwait(false);
                TempData[SelectedKeyTempDataKey] = KeyNumber;
            }

            TempData[ModeTempDataKey] = Mode;
            await LoadKeyTypesAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }
    }

    private async Task LoadKeyTypesAsync(CancellationToken cancellationToken)
    {
        KeyTypeOptions = (await _listKeyTypes.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .OrderBy(item => item.TypeCode, StringComparer.OrdinalIgnoreCase)
            .Select(item => new SelectListItem(
                item.TypeCode,
                item.TypeCode,
                string.Equals(item.TypeCode, TypeCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}

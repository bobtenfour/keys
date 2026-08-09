using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeyTypesModel : PageModel
{
    private readonly IListKeyTypesUseCase _listKeyTypes;
    private readonly IActivateKeyTypeUseCase _activate;
    private readonly IRetireKeyTypeUseCase _retire;

    public KeyTypesModel(
        IListKeyTypesUseCase listKeyTypes,
        IActivateKeyTypeUseCase activate,
        IRetireKeyTypeUseCase retire)
    {
        _listKeyTypes = listKeyTypes ?? throw new ArgumentNullException(nameof(listKeyTypes));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    public IReadOnlyList<KeyTypeListItem> KeyTypes { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        KeyTypes = await _listKeyTypes.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(string typeCode, CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(typeCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Key type {typeCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        KeyTypes = await _listKeyTypes.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string typeCode, CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(typeCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Key type {typeCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        KeyTypes = await _listKeyTypes.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}

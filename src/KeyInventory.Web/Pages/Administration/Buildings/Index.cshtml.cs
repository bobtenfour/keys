using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Buildings;

public sealed class IndexModel : PageModel
{
    private readonly IListBuildingsUseCase _list;
    private readonly IActivateBuildingUseCase _activate;
    private readonly IRetireBuildingUseCase _retire;

    public IndexModel(
        IListBuildingsUseCase list,
        IActivateBuildingUseCase activate,
        IRetireBuildingUseCase retire)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    public IReadOnlyList<BuildingListItem> Buildings { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Buildings = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(string buildingCode, CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(buildingCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Building {buildingCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Buildings = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string buildingCode, CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(buildingCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Building {buildingCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Buildings = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}

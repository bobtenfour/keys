using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Buildings;

public sealed class IndexModel : PageModel
{
    private readonly ICreateBuildingUseCase _create;
    private readonly IListBuildingsUseCase _list;

    public IndexModel(ICreateBuildingUseCase create, IListBuildingsUseCase list)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    [BindProperty]
    public string BuildingCode { get; set; } = string.Empty;

    public IReadOnlyList<BuildingListItem> Buildings { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Buildings = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(BuildingCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Building {BuildingCode} was created.";
            BuildingCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Buildings = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}

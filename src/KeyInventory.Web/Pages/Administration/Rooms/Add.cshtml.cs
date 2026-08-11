using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class AddModel : PageModel
{
    private readonly ICreateRoomUseCase _create;

    public AddModel(ICreateRoomUseCase create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(
                    RoomNumber,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Room {RoomNumber.Trim()} was created.";
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }
}

using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class DeleteModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public DeleteModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    [BindProperty(SupportsGet = true)]
    public string Member { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public WorkforceMemberLifecycleItem? Item { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!Item!.Capabilities.CanDelete)
        {
            TempData["ErrorMessage"] = Item.Capabilities.DeleteBlockedReason
                ?? "This workforce member can no longer be deleted. Terminate the membership instead to preserve history.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!ConfirmDelete)
        {
            ErrorMessage = "Confirm deletion before continuing.";
            return Page();
        }

        try
        {
            string displayName = $"{Item!.FirstName} {Item.LastName}";
            await _lifecycle.DeleteWorkforceMemberAsync(Member, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Workforce member \"{displayName}\" was deleted.";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = FormatDeleteError(exception);
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false)
                || Item is null
                || !Item.Capabilities.CanDelete)
            {
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage("./Index");
            }

            return Page();
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Member))
        {
            return false;
        }

        string code = Member.Trim();
        Item = (await _lifecycle.ListWorkforceMembersAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(item.WorkforceMemberCode, code, StringComparison.OrdinalIgnoreCase));
        if (Item is not null)
        {
            Member = Item.WorkforceMemberCode;
        }

        return Item is not null;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This workforce member can no longer be deleted. Terminate the membership instead to preserve history.";
    }
}

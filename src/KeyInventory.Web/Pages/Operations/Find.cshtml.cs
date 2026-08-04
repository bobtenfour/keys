using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class FindModel : PageModel
{
    public string? Query { get; private set; }

    public void OnGet(string? q)
    {
        Query = q;
    }
}

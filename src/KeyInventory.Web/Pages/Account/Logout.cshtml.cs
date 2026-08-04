using KeyInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Account;

[Authorize]
public sealed class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LogoutModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    }

    public async Task<IActionResult> OnPostAsync(string? returnPath = null)
    {
        await _signInManager.SignOutAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(returnPath) && Url.IsLocalUrl(returnPath))
        {
            return LocalRedirect(returnPath);
        }

        return RedirectToPage("/Index");
    }
}

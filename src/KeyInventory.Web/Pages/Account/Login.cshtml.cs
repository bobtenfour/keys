using System.ComponentModel.DataAnnotations;
using KeyInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    internal const bool SessionOnlySignIn = false;

    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    }

    [BindProperty]
    public LoginFormInput Input { get; set; } = new();

    public string? ReturnPath { get; set; }

    public void OnGet([FromQuery(Name = "ReturnUrl")] string? returnPath = null)
    {
        ReturnPath = returnPath;
    }

    public async Task<IActionResult> OnPostAsync([FromQuery(Name = "ReturnUrl")] string? returnPath = null)
    {
        returnPath ??= ReturnPath ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            ReturnPath = returnPath;
            return Page();
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
            Input.UserName,
            Input.Password,
            SessionOnlySignIn,
            lockoutOnFailure: false).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            ReturnPath = returnPath;
            return Page();
        }

        return LocalRedirect(returnPath);
    }
}

public sealed class LoginFormInput
{
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}

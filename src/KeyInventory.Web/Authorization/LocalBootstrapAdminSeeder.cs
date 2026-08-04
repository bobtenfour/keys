using KeyInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KeyInventory.Web.Authorization;

internal static partial class LocalBootstrapAdminSeeder
{
    internal static async Task SeedAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        IOptions<LocalBootstrapAdminOptions> optionsAccessor,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        LocalBootstrapAdminOptions options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "LocalBootstrapAdmin can only be enabled when the environment is Development.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName) ||
            string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "LocalBootstrapAdmin is enabled but UserName, Email, or Password is missing.");
        }

        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? bootstrapUser = await userManager.FindByNameAsync(options.UserName).ConfigureAwait(false);

        if (bootstrapUser is null)
        {
            if (await userManager.FindByEmailAsync(options.Email).ConfigureAwait(false) is not null)
            {
                throw new InvalidOperationException(
                    "LocalBootstrapAdmin was not created because the configured email is already assigned to another account.");
            }

            bootstrapUser = new ApplicationUser
            {
                UserName = options.UserName,
                Email = options.Email,
                EmailConfirmed = true,
            };

            IdentityResult createResult = await userManager.CreateAsync(bootstrapUser, options.Password).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                string errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create local bootstrap admin: {errors}");
            }

            LogBootstrapCreated(logger, options.UserName);
            return;
        }

        await ReconcilePasswordAsync(userManager, bootstrapUser, options.Password, logger).ConfigureAwait(false);
    }

    private static async Task ReconcilePasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser bootstrapUser,
        string configuredPassword,
        ILogger logger)
    {
        if (await userManager.CheckPasswordAsync(bootstrapUser, configuredPassword).ConfigureAwait(false))
        {
            return;
        }

        string resetToken = await userManager.GeneratePasswordResetTokenAsync(bootstrapUser).ConfigureAwait(false);
        IdentityResult resetResult = await userManager.ResetPasswordAsync(bootstrapUser, resetToken, configuredPassword)
            .ConfigureAwait(false);
        if (!resetResult.Succeeded)
        {
            string errors = string.Join(", ", resetResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to reconcile local bootstrap admin password: {errors}");
        }

        LogBootstrapPasswordReconciled(logger, bootstrapUser.UserName);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Local bootstrap admin '{UserName}' was created for Development smoke testing only.")]
    private static partial void LogBootstrapCreated(ILogger logger, string userName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Local bootstrap admin '{UserName}' password was reconciled to the configured Development secret.")]
    private static partial void LogBootstrapPasswordReconciled(ILogger logger, string? userName);
}

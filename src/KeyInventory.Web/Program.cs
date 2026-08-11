using KeyInventory.Infrastructure.Data;
using KeyInventory.Web;
using KeyInventory.Web.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

WebServiceComposition.Configure(builder.Services, builder.Configuration, builder.Environment);

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    bool applyMigrationsOnStartup = app.Configuration.GetValue("KeyInventory:ApplyMigrationsOnStartup", defaultValue: true);
    if (applyMigrationsOnStartup)
    {
        KeyInventoryDbContext dbContext = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }

    await LocalBootstrapAdminSeeder.SeedAsync(
            scope.ServiceProvider,
            app.Environment,
            scope.ServiceProvider.GetRequiredService<IOptions<LocalBootstrapAdminOptions>>(),
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("LocalBootstrapAdmin"))
        .ConfigureAwait(false);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/health/ready", async (KeyInventoryDbContext dbContext, CancellationToken cancellationToken) =>
    {
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return canConnect
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    })
    .AllowAnonymous();

await app.RunAsync().ConfigureAwait(false);

namespace KeyInventory.Web
{
    public partial class Program
    {
    }
}

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
    KeyInventoryDbContext dbContext = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
    await dbContext.Database.MigrateAsync().ConfigureAwait(false);

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

await app.RunAsync().ConfigureAwait(false);

namespace KeyInventory.Web
{
    public partial class Program
    {
    }
}

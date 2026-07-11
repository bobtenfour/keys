using KeyInventory.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorPages();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

namespace KeyInventory.Web
{
    public partial class Program
    {
    }
}

using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Identity;
using KeyInventory.Web.Authorization;
using Microsoft.AspNetCore.Identity;

namespace KeyInventory.Web;

public static class WebServiceComposition
{
    public static void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string connectionString = configuration.GetConnectionString("KeyInventory")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KeyInventory is required and must target SQL Server.");

        services
            .AddOptions<LocalBootstrapAdminOptions>()
            .Bind(configuration.GetSection(LocalBootstrapAdminOptions.SectionName));

        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Keys");
            options.Conventions.AuthorizeFolder("/Loans");
            options.Conventions.AllowAnonymousToPage("/Account/Login");
            options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
            options.Conventions.AllowAnonymousToPage("/Index");
            options.Conventions.AllowAnonymousToPage("/Error");
        });

        LoanVerticalComposition.AddLoanVertical(services, connectionString);

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<KeyInventory.Infrastructure.Data.KeyInventoryDbContext>()
            .AddDefaultTokenProviders();

        if (environment.IsDevelopment())
        {
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 4;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            });
        }

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = ".KeyInventory.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.SlidingExpiration = true;
        });
    }
}

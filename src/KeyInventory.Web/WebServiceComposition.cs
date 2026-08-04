using KeyInventory.Infrastructure;

namespace KeyInventory.Web;

public static class WebServiceComposition
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("KeyInventory")
            ?? "Data Source=keyinventory-local.db";

        services.AddRazorPages();
        LoanVerticalComposition.AddLoanVertical(services, connectionString);
    }
}

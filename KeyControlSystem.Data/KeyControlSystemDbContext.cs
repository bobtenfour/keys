using Microsoft.EntityFrameworkCore;

namespace KeyControlSystem.Data;

public sealed class KeyControlSystemDbContext(DbContextOptions<KeyControlSystemDbContext> options) : DbContext(options)
{
    public const string ConnectionString =
        "Server=localhost;Database=KeyControlSystemDev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
    }
}

using KeyInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyInventoryDbContext : IdentityDbContext<ApplicationUser>
{
    public KeyInventoryDbContext(DbContextOptions<KeyInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<KeyTypeEntity> KeyTypes => Set<KeyTypeEntity>();

    public DbSet<KeyAssetEntity> KeyAssets => Set<KeyAssetEntity>();

    public DbSet<LoanEntity> Loans => Set<LoanEntity>();

    public DbSet<ReturnEntity> Returns => Set<ReturnEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(KeyInventoryDbContext).Assembly);
    }
}

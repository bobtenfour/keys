using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyInventoryDbContext : DbContext
{
    public KeyInventoryDbContext(DbContextOptions<KeyInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<KeyTypeEntity> KeyTypes => Set<KeyTypeEntity>();

    public DbSet<KeyAssetEntity> KeyAssets => Set<KeyAssetEntity>();

    public DbSet<LoanEntity> Loans => Set<LoanEntity>();

    public DbSet<ReturnEntity> Returns => Set<ReturnEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KeyInventoryDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyInventoryDbContextFactory : IDesignTimeDbContextFactory<KeyInventoryDbContext>
{
    public KeyInventoryDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<KeyInventoryDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite("Data Source=keyinventory-local.db");
        return new KeyInventoryDbContext(optionsBuilder.Options);
    }
}

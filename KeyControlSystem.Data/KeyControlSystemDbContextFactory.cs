using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KeyControlSystem.Data;

public sealed class KeyControlSystemDbContextFactory : IDesignTimeDbContextFactory<KeyControlSystemDbContext>
{
    public KeyControlSystemDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KeyControlSystemDbContext>()
            .UseSqlServer(KeyControlSystemDbContext.ConnectionString)
            .Options;

        return new KeyControlSystemDbContext(options);
    }
}

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

    public DbSet<KeyRoomAssignmentEntity> KeyRoomAssignments => Set<KeyRoomAssignmentEntity>();

    public DbSet<LoanEntity> Loans => Set<LoanEntity>();

    public DbSet<ReturnEntity> Returns => Set<ReturnEntity>();

    public DbSet<PartyEntity> Parties => Set<PartyEntity>();

    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();

    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();

    public DbSet<WorkforceMemberEntity> WorkforceMembers => Set<WorkforceMemberEntity>();

    public DbSet<WorkAssignmentEntity> WorkAssignments => Set<WorkAssignmentEntity>();

    public DbSet<OperatorAuditRecordEntity> OperatorAuditRecords => Set<OperatorAuditRecordEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(KeyInventoryDbContext).Assembly);
    }
}

using System.Reflection;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class MigrationBoundaryTests
{
    [Fact]
    public void InfrastructureOwnsDbContextAndMigrationTypes()
    {
        Assembly infrastructureAssembly = typeof(KeyInventoryDbContext).Assembly;

        Assert.NotNull(infrastructureAssembly.GetType(typeof(KeyInventoryDbContext).FullName!));
        Assert.NotNull(infrastructureAssembly.GetType(typeof(KeyInventoryDbContextFactory).FullName!));

        Type[] migrationTypes = infrastructureAssembly
            .GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .ToArray();

        Assert.NotEmpty(migrationTypes);
    }

    [Fact]
    public void DomainDoesNotReferenceEfCorePersistenceTypes()
    {
        Assembly domainAssembly = typeof(KeyInventory.Domain.UtcTimestamp).Assembly;

        string[] efReferences = domainAssembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(efReferences);
    }

    [Fact]
    public void ApplicationDoesNotReferenceEfCorePersistenceTypes()
    {
        Assembly applicationAssembly = typeof(KeyInventory.Application.Loans.ILoanLookupPort).Assembly;

        string[] efReferences = applicationAssembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(efReferences);
    }

    [Fact]
    public void WebDoesNotIntroducePersistenceAuthorityTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] persistenceTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "DbContext", "EntityFramework", "Migration", "KeyTypeEntity", "LoanEntity"))
            .ToArray();

        Assert.Empty(persistenceTypes);
    }

    [Fact]
    public void EfModelIncludesAuthorizedEntityMappingsOnly()
    {
        using KeyInventoryDbContext context = CreateContext();
        IModel model = context.Model;

        string[] tableNames = model
            .GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(name => name is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("KeyAssets", tableNames);
        Assert.Contains("KeyTypes", tableNames);
        Assert.Contains("Loans", tableNames);
        Assert.Contains("Returns", tableNames);
        Assert.Contains("Parties", tableNames);
        Assert.Contains("Organizations", tableNames);
        Assert.Contains("Departments", tableNames);
        Assert.Contains("Buildings", tableNames);
        Assert.Contains("Rooms", tableNames);
        Assert.Contains("WorkforceMembers", tableNames);
        Assert.Contains("WorkAssignments", tableNames);
        Assert.Contains("KeyRoomAssignments", tableNames);
        Assert.Contains("AspNetUsers", tableNames);

        Type[] clrTypes = model.GetEntityTypes().Select(entityType => entityType.ClrType).ToArray();
        Assert.Contains(typeof(KeyTypeEntity), clrTypes);
        Assert.Contains(typeof(KeyAssetEntity), clrTypes);
        Assert.Contains(typeof(KeyRoomAssignmentEntity), clrTypes);
        Assert.Contains(typeof(LoanEntity), clrTypes);
        Assert.Contains(typeof(ReturnEntity), clrTypes);
        Assert.Contains(typeof(PartyEntity), clrTypes);
        Assert.Contains(typeof(OrganizationEntity), clrTypes);
        Assert.Contains(typeof(DepartmentEntity), clrTypes);
        Assert.Contains(typeof(BuildingEntity), clrTypes);
        Assert.Contains(typeof(RoomEntity), clrTypes);
        Assert.Contains(typeof(WorkforceMemberEntity), clrTypes);
        Assert.Contains(typeof(WorkAssignmentEntity), clrTypes);
        Assert.Contains(typeof(KeyInventory.Infrastructure.Identity.ApplicationUser), clrTypes);
        Assert.DoesNotContain(typeof(KeyInventory.Domain.Catalog.KeySeries), clrTypes);
        Assert.DoesNotContain(typeof(KeyInventory.Domain.Catalog.Lock), clrTypes);
        Assert.DoesNotContain(typeof(KeyInventory.Domain.Catalog.Location), clrTypes);
        Assert.DoesNotContain(typeof(KeyInventory.Domain.Identity.SecurityPrincipal), clrTypes);
        Assert.DoesNotContain(typeof(KeyInventory.Domain.Audit.AuditEvent), clrTypes);
    }

    [Fact]
    public void DesignTimeFactoryUsesSqlServer()
    {
        KeyInventoryDbContextFactory factory = new();
        using KeyInventoryDbContext context = factory.CreateDbContext([]);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public void UtcTimestampPropertiesMapAsDateTimeOffsetWithoutConversion()
    {
        using KeyInventoryDbContext context = CreateContext();
        IModel model = context.Model;

        AssertDateTimeOffsetProperty(model, typeof(LoanEntity), nameof(LoanEntity.IssuedAtUtc));
        AssertDateTimeOffsetProperty(model, typeof(LoanEntity), nameof(LoanEntity.DueAtUtc));
        AssertDateTimeOffsetProperty(model, typeof(ReturnEntity), nameof(ReturnEntity.ReturnedAtUtc));
    }

    private static void AssertDateTimeOffsetProperty(IModel model, Type clrType, string propertyName)
    {
        IEntityType entityType = model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"Entity type {clrType.Name} was not found.");
        IProperty property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        Assert.Equal(typeof(DateTimeOffset), property.ClrType);
        Assert.Null(property.GetValueConverter());
    }

    private static KeyInventoryDbContext CreateContext()
    {
        string connectionString = KeyInventorySqlServerTestConnection.Require();
        DbContextOptions<KeyInventoryDbContext> options = new DbContextOptionsBuilder<KeyInventoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new KeyInventoryDbContext(options);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

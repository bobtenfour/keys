using System.Reflection;
using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Web.Pages.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyRoomAssignmentBoundaryTests
{
    [Fact]
    public void CatalogKeyRoomsPageConsumesApplicationUseCaseWithoutDbContext()
    {
        ConstructorInfo ctor = typeof(KeyRoomsModel).GetConstructors().Single();
        Assert.Contains(ctor.GetParameters(), parameter => parameter.ParameterType == typeof(IKeyRoomAssignmentUseCase));
        Assert.DoesNotContain(
            ctor.GetParameters(),
            parameter =>
                parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal)
                || (parameter.ParameterType.Namespace?.StartsWith("KeyInventory.Infrastructure", StringComparison.Ordinal) ?? false)
                || parameter.ParameterType == typeof(IKeyRoomAssignmentPersistencePort));
    }

    [Fact]
    public void SliceDoesNotIntroduceHistoryMasterKeyOrSecondPersistence()
    {
        Assembly[] assemblies =
        [
            typeof(IKeyRoomAssignmentUseCase).Assembly,
            typeof(LoanVerticalComposition).Assembly,
            typeof(KeyInventory.Web.Program).Assembly
        ];

        string[] prohibited = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "AssignmentHistory",
                "KeyRoomHistory",
                "MasterKey",
                "SubMaster",
                "Reports2",
                "Sqlite",
                "InMemory",
                "Elasticsearch"))
            .ToArray();

        Assert.Empty(prohibited);
    }

    [Fact]
    public void CompositionRegistersRoomAssignmentAuthority()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration["ConnectionStrings:KeyInventory"] = KeyInventorySqlServerTestConnection.Require();
        KeyInventory.Web.WebServiceComposition.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IKeyRoomAssignmentUseCase>());
        Assert.NotNull(provider.GetService<IKeyRoomAssignmentPersistencePort>());
    }

    [Fact]
    public void KeyRoomAssignmentMappingHasNoLockForeignKeyAndNoBuildingOnKeyAsset()
    {
        string connectionString = KeyInventorySqlServerTestConnection.Require();
        DbContextOptions<KeyInventoryDbContext> options = new DbContextOptionsBuilder<KeyInventoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        using KeyInventoryDbContext context = new(options);

        Assert.Null(typeof(KeyAssetEntity).GetProperty("Building"));
        Assert.Null(typeof(KeyAssetEntity).GetProperty("BuildingCode"));
        Assert.Null(typeof(KeyAsset).GetProperty("Building"));
        Assert.Null(typeof(KeyAsset).GetProperty("BuildingCode"));

        var assignmentEntity = context.Model.FindEntityType(typeof(KeyRoomAssignmentEntity));
        Assert.NotNull(assignmentEntity);
        Assert.Equal("KeyRoomAssignments", assignmentEntity.GetTableName());
        Assert.DoesNotContain(
            assignmentEntity.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType.Name.Contains("Lock", StringComparison.Ordinal));
        Assert.Contains(
            assignmentEntity.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(RoomEntity));
        Assert.Contains(
            assignmentEntity.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(KeyAssetEntity));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

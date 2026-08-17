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

public sealed class ClassificationAccessAuthorityBoundaryTests
{
    [Fact]
    public void KeyRoomsPageAndAssignUseCasesAreAbsent()
    {
        Assert.Null(typeof(KeysModel).Assembly.GetType("KeyInventory.Web.Pages.Catalog.KeyRoomsModel"));
        Assert.Null(typeof(IKeyAccessResolutionPort).Assembly.GetType(
            "KeyInventory.Application.Catalog.IKeyAccessPatternRoomAssignmentUseCase"));
        Assert.Null(typeof(IKeyAccessResolutionPort).Assembly.GetType(
            "KeyInventory.Application.Catalog.IKeyAccessPatternRoomAssignmentPersistencePort"));
        Assert.Null(typeof(LoanVerticalComposition).Assembly.GetType(
            "KeyInventory.Infrastructure.Data.KeyAccessPatternRoomAssignmentEntity"));
    }

    [Fact]
    public void SliceDoesNotIntroduceHistoryOrSecondPersistence()
    {
        Assembly[] assemblies =
        [
            typeof(IKeyAccessResolutionPort).Assembly,
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
                "SubMaster",
                "Reports2",
                "Sqlite",
                "InMemory",
                "Elasticsearch",
                "KeyAccessPatternRoomAssignment"))
            .ToArray();

        Assert.Empty(prohibited);
    }

    [Fact]
    public void CompositionRegistersAccessResolutionAuthority()
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
        Assert.NotNull(provider.GetService<IKeyAccessResolutionPort>());
        Assert.Null(typeof(IKeyAccessResolutionPort).Assembly.GetType(
            "KeyInventory.Application.Catalog.IKeyAccessPatternRoomAssignmentUseCase"));
    }

    [Fact]
    public void KeyAccessPatternStoresRoomCodeAndKeyAssetDoesNot()
    {
        string connectionString = KeyInventorySqlServerTestConnection.Require();
        DbContextOptions<KeyInventoryDbContext> options = new DbContextOptionsBuilder<KeyInventoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        using KeyInventoryDbContext context = new(options);

        Assert.Null(typeof(KeyAssetEntity).GetProperty("Building"));
        Assert.Null(typeof(KeyAssetEntity).GetProperty("BuildingCode"));
        Assert.Null(typeof(KeyAssetEntity).GetProperty("RoomCode"));
        Assert.Null(typeof(KeyAsset).GetProperty("Building"));
        Assert.Null(typeof(KeyAsset).GetProperty("BuildingCode"));
        Assert.NotNull(typeof(KeyAccessPatternEntity).GetProperty("RoomCode"));
        Assert.NotNull(typeof(KeyAccessPattern).GetProperty("RoomCode"));
        Assert.NotNull(typeof(KeyAccessPattern).GetProperty("OpensAllRooms"));

        var patternEntity = context.Model.FindEntityType(typeof(KeyAccessPatternEntity));
        Assert.NotNull(patternEntity);
        Assert.Equal("KeyAccessPatterns", patternEntity.GetTableName());
        Assert.Contains(
            patternEntity.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(RoomEntity)
                && fk.Properties.Any(property => property.Name == "RoomCode"));
        Assert.Null(context.Model.FindEntityType(
            "KeyInventory.Infrastructure.Data.KeyAccessPatternRoomAssignmentEntity"));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

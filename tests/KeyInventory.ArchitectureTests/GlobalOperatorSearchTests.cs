using System.Reflection;
using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Lookup;
using KeyInventory.Web.Pages;
using KeyInventory.Web.Pages.Operations;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class GlobalOperatorSearchTests : IAsyncLifetime
{
    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        string connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();
        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, connectionString);
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        if (_services is null)
        {
            return;
        }

        using (IServiceScope scope = _services.CreateScope())
        {
            KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
            await db.Database.EnsureDeletedAsync().ConfigureAwait(true);
        }

        await _services.DisposeAsync().ConfigureAwait(true);
        _services = null;
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }

    [Fact]
    public void ExactlyOneGlobalSearchUseCaseAndNoSecondStore()
    {
        Assembly application = typeof(IGlobalOperatorSearchUseCase).Assembly;
        Assembly infrastructure = typeof(GlobalOperatorSearchAdapter).Assembly;
        Assembly web = typeof(SearchModel).Assembly;

        Assert.Single(
            application.GetTypes(),
            type => type.IsClass && !type.IsAbstract && typeof(IGlobalOperatorSearchUseCase).IsAssignableFrom(type));

        string[] prohibited = application.GetTypes()
            .Concat(infrastructure.GetTypes())
            .Concat(web.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(
                name,
                "GlobalSearchEntity",
                "SearchIndex",
                "Elasticsearch",
                "Lucene",
                "FuzzySearchEngine"))
            .ToArray();
        Assert.Empty(prohibited);

        ConstructorInfo searchCtor = typeof(SearchModel).GetConstructors().Single();
        Assert.Contains(searchCtor.GetParameters(), p => p.ParameterType == typeof(IGlobalOperatorSearchUseCase));
        Assert.DoesNotContain(
            searchCtor.GetParameters(),
            p => (p.ParameterType.FullName ?? p.ParameterType.Name).Contains("DbContext", StringComparison.Ordinal));

        ConstructorInfo findCtor = typeof(FindModel).GetConstructors().Single();
        Assert.Contains(findCtor.GetParameters(), p => p.ParameterType == typeof(IOperationalKeyLookupUseCase));
        Assert.DoesNotContain(findCtor.GetParameters(), p => p.ParameterType == typeof(IGlobalOperatorSearchUseCase));
    }

    [Fact]
    public void CompositionRegistersGlobalSearchAndHeaderTargetsSearchPage()
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
        Assert.NotNull(provider.GetService<IGlobalOperatorSearchUseCase>());
        Assert.NotNull(provider.GetService<IGlobalOperatorSearchPort>());
        Assert.IsType<GlobalOperatorSearchAdapter>(provider.GetRequiredService<IGlobalOperatorSearchPort>());

        string layout = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Shared/_Layout.cshtml"));
        Assert.Contains("asp-page=\"/Search\"", layout, StringComparison.Ordinal);
        Assert.Contains("Search name, UIN, Room #, KEY #, or MEDECO", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-page=\"/Operations/Find\"", layout, StringComparison.Ordinal);

        string composition = File.ReadAllText(Path.Combine(RepoRoot(), "src/KeyInventory.Web/WebServiceComposition.cs"));
        Assert.Contains("AuthorizePage(\"/Search\")", composition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersonNameAndUinReturnIdentityDepartmentAndCurrentKeys()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();
        SeededPeopleKeys seeded = await SeedPeopleAndKeysAsync(scope.ServiceProvider, "gos-p").ConfigureAwait(true);

        GlobalOperatorSearchResult byName = await search.SearchAsync("Brian", CancellationToken.None).ConfigureAwait(true);
        GlobalPersonSearchHit brian = Assert.Single(byName.People, person => person.Uin == seeded.BrianUin);
        Assert.Equal("Brian", brian.FirstName);
        Assert.Equal("Holder", brian.LastName);
        Assert.Equal(seeded.DepartmentCode, brian.DepartmentCode);
        Assert.Equal("Active", brian.Status);
        Assert.Contains(brian.WorkAssignments, assignment => assignment.RoomNumber == seeded.Room410);
        GlobalPersonCurrentKey held = Assert.Single(brian.CurrentKeys);
        Assert.Equal(seeded.KeyNumberA, held.KeyNumber);
        Assert.Equal("27", held.MedecoKeyCode);
        Assert.Contains(held.OpenedRooms, room => room.RoomNumber == seeded.Room410);

        GlobalOperatorSearchResult byUin = await search.SearchAsync(seeded.BrianUin, CancellationToken.None)
            .ConfigureAwait(true);
        GlobalPersonSearchHit brianByUin = Assert.Single(byUin.People);
        Assert.Equal(brian.WorkforceMemberCode, brianByUin.WorkforceMemberCode);
        Assert.Equal(brian.CurrentKeys.Count, brianByUin.CurrentKeys.Count);
        Assert.Equal(brian.CurrentKeys[0].KeyNumber, brianByUin.CurrentKeys[0].KeyNumber);
        Assert.Equal(brian.CurrentKeys[0].MedecoKeyCode, brianByUin.CurrentKeys[0].MedecoKeyCode);
    }

    [Fact]
    public async Task PersonWithoutKeysIsValidResultNotZeroResults()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();
        SeededPeopleKeys seeded = await SeedPeopleAndKeysAsync(scope.ServiceProvider, "gos-z").ConfigureAwait(true);

        GlobalOperatorSearchResult result = await search.SearchAsync("Casey", CancellationToken.None).ConfigureAwait(true);
        Assert.True(result.HasAnyResults);
        GlobalPersonSearchHit casey = Assert.Single(result.People, person => person.Uin == seeded.CaseyUin);
        Assert.Empty(casey.CurrentKeys);
        Assert.Contains(casey.WorkAssignments, assignment => assignment.RoomNumber == seeded.Room410);

        string page = await File.ReadAllTextAsync(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Search.cshtml"))
            .ConfigureAwait(true);
        Assert.Contains("Search Results", page, StringComparison.Ordinal);
        Assert.Contains("Search results for", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member details", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member keys", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<form method=\"get\" asp-page=\"/Search\"", page, StringComparison.Ordinal);
        Assert.Contains("Room Assignment", page, StringComparison.Ordinal);
        Assert.Contains("Current Key Custody", page, StringComparison.Ordinal);
        Assert.Contains("No keys currently issued.", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MultiplePeopleWithSimilarNamesAreDistinct()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();
        SeededPeopleKeys seeded = await SeedPeopleAndKeysAsync(scope.ServiceProvider, "gos-m").ConfigureAwait(true);

        GlobalOperatorSearchResult result = await search.SearchAsync("Bri", CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(result.People, person => person.Uin == seeded.BrianUin);
        Assert.Contains(result.People, person => person.Uin == seeded.BriannaUin);
        Assert.Equal(2, result.People.Count(person => person.FirstName.StartsWith("Bri", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ClosedLoanDoesNotAppearAsCurrentlyIssued()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        SeededPeopleKeys seeded = await SeedPeopleAndKeysAsync(scope.ServiceProvider, "gos-c").ConfigureAwait(true);

        await completeReturn.ExecuteAsync(
                "ret-gos-c",
                seeded.BrianLoanCode,
                DateTimeOffset.UtcNow,
                CancellationToken.None)
            .ConfigureAwait(true);

        GlobalOperatorSearchResult result = await search.SearchAsync("Brian", CancellationToken.None).ConfigureAwait(true);
        GlobalPersonSearchHit brian = Assert.Single(result.People, person => person.Uin == seeded.BrianUin);
        Assert.Empty(brian.CurrentKeys);
    }

    [Fact]
    public async Task RoomKeyAndMedecoTypedGroupsComposeAuthoritativeFacts()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();
        SeededPeopleKeys seeded = await SeedPeopleAndKeysAsync(scope.ServiceProvider, "gos-k").ConfigureAwait(true);

        GlobalOperatorSearchResult byRoom = await search.SearchAsync(seeded.Room410, CancellationToken.None)
            .ConfigureAwait(true);
        GlobalRoomSearchHit room = Assert.Single(byRoom.Rooms, item => item.RoomNumber == seeded.Room410);
        Assert.Contains(seeded.KeyNumberA, room.OpeningKeyNumbers);
        Assert.Contains(seeded.KeyNumberMaster, room.OpeningKeyNumbers);

        GlobalOperatorSearchResult byKey = await search.SearchAsync(seeded.KeyNumberA, CancellationToken.None)
            .ConfigureAwait(true);
        GlobalKeyNumberSearchHit key = Assert.Single(byKey.KeyNumbers, item => item.KeyNumber == seeded.KeyNumberA);
        Assert.Contains(key.OpenedRooms, opened => opened.RoomNumber == seeded.Room410);
        Assert.Contains(key.Copies, copy => copy.MedecoKeyCode == "26" && copy.AvailabilityStatus == OperationalKeyAvailability.Available);
        Assert.Contains(key.Copies, copy => copy.MedecoKeyCode == "27" && copy.AvailabilityStatus == OperationalKeyAvailability.Issued);

        GlobalOperatorSearchResult byMedeco = await search.SearchAsync("27", CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(
            byMedeco.MedecoCopies,
            copy => copy.KeyNumber == seeded.KeyNumberA
                && copy.MedecoKeyCode == "27"
                && copy.CurrentHolder is not null);
        Assert.Contains(
            byMedeco.MedecoCopies,
            copy => copy.KeyNumber == seeded.KeyNumberB && copy.MedecoKeyCode == "27");
    }

    [Fact]
    public async Task ZeroResultsUsesGlobalEmptyStateContract()
    {
        using IServiceScope scope = CreateScope();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();

        GlobalOperatorSearchResult result = await search
            .SearchAsync("zz-no-match-xyz", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.False(result.HasAnyResults);

        string page = await File.ReadAllTextAsync(Path.Combine(RepoRoot(), "src/KeyInventory.Web/Pages/Search.cshtml"))
            .ConfigureAwait(true);
        Assert.Contains("Search Results", page, StringComparison.Ordinal);
        Assert.Contains("Search results for", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member details", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Member keys", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<form method=\"get\" asp-page=\"/Search\"", page, StringComparison.Ordinal);
        Assert.Contains("No results found for", page, StringComparison.Ordinal);
        Assert.Contains("Search by name, UIN, Room #, KEY #, or MEDECO", page, StringComparison.Ordinal);
        Assert.DoesNotContain("No matching keys", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Browse Keys", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResultsRemainBoundedPerCategory()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IRegisterWorkforceMemberUseCase register = scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        IGlobalOperatorSearchUseCase search = scope.ServiceProvider.GetRequiredService<IGlobalOperatorSearchUseCase>();

        string dept = "gos-bound-dept";
        await createDept.ExecuteAsync(dept, CancellationToken.None).ConfigureAwait(true);
        for (int i = 0; i < 30; i++)
        {
            await register.ExecuteAsync(
                    "Bound",
                    $"Person{i:D2}",
                    (900000000 + i).ToString("D9", System.Globalization.CultureInfo.InvariantCulture),
                    "Employee",
                    dept,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }

        GlobalOperatorSearchResult result = await search.SearchAsync("Bound", CancellationToken.None).ConfigureAwait(true);
        Assert.True(result.People.Count <= IGlobalOperatorSearchUseCase.DefaultMaxPerCategory);
    }

    private static async Task<SeededPeopleKeys> SeedPeopleAndKeysAsync(IServiceProvider services, string prefix)
    {
        ICreateDepartmentUseCase createDept = services.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = services.GetRequiredService<ICreateRoomUseCase>();
        IRegisterWorkforceMemberUseCase register = services.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        ICreateWorkAssignmentUseCase createAssignment = services.GetRequiredService<ICreateWorkAssignmentUseCase>();
        ICreateKeyAssetUseCase createKey = services.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = services.GetRequiredService<IIssueLoanUseCase>();

        string dept = $"{prefix}-fac";
        string room410Number = $"{prefix}-410D";
        string room411Number = $"{prefix}-411A";
        string keyA = $"{prefix}-66800";
        string keyMaster = $"{prefix}-MASTER1";
        string keyB = $"{prefix}-54970";

        await createDept.ExecuteAsync(dept, CancellationToken.None).ConfigureAwait(true);
        string room410 = await createRoom.ExecuteAsync(dept, room410Number, "Office", CancellationToken.None).ConfigureAwait(true);
        string room411 = await createRoom.ExecuteAsync(dept, room411Number, "Lab", CancellationToken.None).ConfigureAwait(true);

        string brianUin = UniqueUin(prefix, 11);
        string caseyUin = UniqueUin(prefix, 12);
        string briannaUin = UniqueUin(prefix, 13);

        string brian = await register.ExecuteAsync("Brian", "Holder", brianUin, "Employee", dept, CancellationToken.None)
            .ConfigureAwait(true);
        string casey = await register.ExecuteAsync("Casey", "Quiet", caseyUin, "Employee", dept, CancellationToken.None)
            .ConfigureAwait(true);
        string brianna = await register.ExecuteAsync("Brianna", "Other", briannaUin, "Employee", dept, CancellationToken.None)
            .ConfigureAwait(true);

        await createAssignment.ExecuteAsync(brian, room410, CancellationToken.None)
            .ConfigureAwait(true);
        await createAssignment.ExecuteAsync(casey, room410, CancellationToken.None)
            .ConfigureAwait(true);
        await createAssignment.ExecuteAsync(brianna, room411, CancellationToken.None)
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(services, keyA, "26", KeyAccessClassification.Regular, room410, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(services, keyA, "27", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(services, keyMaster, "01", KeyAccessClassification.Master, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(services, keyB, "27", KeyAccessClassification.Regular, room411, CancellationToken.None).ConfigureAwait(true);

        string loanCode = $"{prefix}-loan-27";
        DateTimeOffset issued = new(2026, 8, 13, 15, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                loanCode,
                keyA,
                "27",
                brian,
                "Department",
                dept,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        return new SeededPeopleKeys(
            dept,
            brianUin,
            caseyUin,
            briannaUin,
            loanCode,
            room410Number,
            keyA,
            keyMaster,
            keyB);
    }

    private static string UniqueUin(string prefix, int salt)
    {
        int hash = Math.Abs(HashCode.Combine(prefix, salt)) % 1_000_000_000;
        return hash.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "KeyInventory.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static bool ContainsAny(string value, params string[] terms)
        => terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private sealed record SeededPeopleKeys(
        string DepartmentCode,
        string BrianUin,
        string CaseyUin,
        string BriannaUin,
        string BrianLoanCode,
        string Room410,
        string KeyNumberA,
        string KeyNumberMaster,
        string KeyNumberB);
}

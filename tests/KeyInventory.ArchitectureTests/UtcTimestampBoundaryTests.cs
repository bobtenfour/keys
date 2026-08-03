using System.Reflection;
using KeyInventory.Domain;
using KeyInventory.Domain.Audit;
using KeyInventory.Domain.Identity;
using KeyInventory.Domain.Loans;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class UtcTimestampBoundaryTests
{
    [Fact]
    public void AuthoritativeDomainTimestampPropertiesUseUtcNamingAndDateTimeOffset()
    {
        AssertUtcTimestampProperty(typeof(Loan), "IssuedAtUtc");
        AssertUtcTimestampProperty(typeof(Loan), "DueAtUtc");
        AssertUtcTimestampProperty(typeof(Return), "ReturnedAtUtc");
        AssertUtcTimestampProperty(typeof(AuditEvent), "OccurredAtUtc");
        AssertUtcTimestampProperty(typeof(PrincipalRoleAssignment), "EffectiveFromUtc");

        PropertyInfo? effectiveTo = typeof(PrincipalRoleAssignment).GetProperty("EffectiveToUtc");
        Assert.NotNull(effectiveTo);
        Assert.Equal(typeof(DateTimeOffset?), effectiveTo.PropertyType);
        Assert.EndsWith("Utc", effectiveTo.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainProvidesSingleSharedUtcTimestampHelper()
    {
        Type helper = typeof(UtcTimestamp);

        Assert.True(helper.IsClass);
        Assert.True(helper.IsAbstract && helper.IsSealed);
        Assert.Equal("KeyInventory.Domain", helper.Namespace);

        MethodInfo? require = helper.GetMethod(
            "Require",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(DateTimeOffset), typeof(string)],
            modifiers: null);

        Assert.NotNull(require);
        Assert.Equal(typeof(DateTimeOffset), require.ReturnType);
    }

    [Fact]
    public void InfrastructureDoesNotIntroduceUtcBusinessTimestampAuthorityTypes()
    {
        Assembly infrastructureAssembly = Assembly.Load("KeyInventory.Infrastructure");

        string[] utcAuthorityTypes = infrastructureAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "UtcTimestamp", "UtcInstant", "UtcTime"))
            .ToArray();

        Assert.Empty(utcAuthorityTypes);
    }

    [Fact]
    public void WebDoesNotIntroduceUtcBusinessTimestampAuthorityTypes()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;

        string[] utcAuthorityTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => ContainsAny(name, "UtcTimestamp", "UtcInstant", "UtcTime"))
            .ToArray();

        Assert.Empty(utcAuthorityTypes);
    }

    private static void AssertUtcTimestampProperty(Type type, string propertyName)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTimeOffset), property.PropertyType);
        Assert.True(
            property.Name.EndsWith("Utc", StringComparison.Ordinal)
            || property.Name.EndsWith("AtUtc", StringComparison.Ordinal));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

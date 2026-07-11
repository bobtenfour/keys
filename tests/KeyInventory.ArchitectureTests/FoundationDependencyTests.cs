using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class FoundationDependencyTests
{
    [Theory]
    [MemberData(nameof(ProjectReferenceRules))]
    public void ProjectDependenciesMatchAllowedLayering(string projectFile, string[] allowedProjectReferences)
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot, projectFile);
        XDocument project = XDocument.Load(projectPath);

        string[] keyInventoryReferences = project.Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")?.Value))
            .Where(name => name is not null && name.StartsWith("KeyInventory.", StringComparison.Ordinal))
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedProjectReferences.Order(StringComparer.Ordinal), keyInventoryReferences);
    }

    public static TheoryData<string, string[]> ProjectReferenceRules { get; } = new()
    {
        { @"src\KeyInventory.Domain\KeyInventory.Domain.csproj", [] },
        { @"src\KeyInventory.Application\KeyInventory.Application.csproj", ["KeyInventory.Domain"] },
        { @"src\KeyInventory.Infrastructure\KeyInventory.Infrastructure.csproj", ["KeyInventory.Application"] },
        { @"src\KeyInventory.Web\KeyInventory.Web.csproj", ["KeyInventory.Application", "KeyInventory.Infrastructure"] }
    };

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "KeyInventory.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("KeyInventory.sln was not found.");
    }
}

using System.Reflection;

namespace MiniErp.UnitTests.Architecture;

// Enforces the dependency rule from ADR-0002: the core (Domain, Application) never depends on
// adapters or on concrete technologies. Checks compiled references, i.e. what the code actually uses.
// Limitation: constants are inlined by the compiler and leave no assembly reference.
public class DependencyRuleTests
{
    private static readonly string[] TechnologyPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MySql",
    ];

    [Fact]
    public void Domain_ReferencedAssemblies_DoNotIncludeOtherMiniErpProjects()
    {
        IEnumerable<string> miniErpReferences = GetReferencedAssemblyNames("MiniErp.Domain")
            .Where(name => name.StartsWith("MiniErp.", StringComparison.Ordinal));

        Assert.Empty(miniErpReferences);
    }

    [Fact]
    public void Application_ReferencedAssemblies_OnlyIncludeDomainFromMiniErp()
    {
        IEnumerable<string> miniErpReferences = GetReferencedAssemblyNames("MiniErp.Application")
            .Where(name => name.StartsWith("MiniErp.", StringComparison.Ordinal));

        Assert.All(miniErpReferences, name => Assert.Equal("MiniErp.Domain", name));
    }

    [Theory]
    [InlineData("MiniErp.Domain")]
    [InlineData("MiniErp.Application")]
    public void CoreAssembly_ReferencedAssemblies_DoNotIncludeTechnologyFrameworks(string assemblyName)
    {
        IEnumerable<string> technologyReferences = GetReferencedAssemblyNames(assemblyName)
            .Where(name => TechnologyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));

        Assert.Empty(technologyReferences);
    }

    private static IEnumerable<string> GetReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(new AssemblyName(assemblyName))
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);
}

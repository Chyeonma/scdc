using System.Xml.Linq;

namespace SCDC.Api.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Feature_modules_reference_only_contracts_and_building_blocks()
    {
        var root = FindRepositoryRoot();
        var moduleDirectory = Path.Combine(root, "services", "Modules");
        var projects = Directory.GetFiles(moduleDirectory, "*.csproj", SearchOption.AllDirectories);
        Assert.Equal(4, projects.Length);
        foreach (var project in projects)
        {
            var document = XDocument.Load(project);
            var references = document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value.Replace('\\', '/'))
                .ToArray();
            Assert.Equal(2, references.Length);
            Assert.Contains(references, path => path?.EndsWith("/SCDC.Contracts/SCDC.Contracts.csproj", StringComparison.Ordinal) == true);
            Assert.Contains(references, path => path?.EndsWith("/SCDC.BuildingBlocks/SCDC.BuildingBlocks.csproj", StringComparison.Ordinal) == true);
            Assert.DoesNotContain(references, path => path?.Contains("/Modules/", StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SCDC.slnx"))) return directory.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate SCDC.slnx from the test output directory.");
    }
}

namespace Maliev.OrderService.Tests.Contract;

/// <summary>
/// Guards the stable v1 route and authorization source contract during the main promotion.
/// </summary>
public sealed class ApiSourceContractTests
{
    /// <summary>
    /// Verifies controllers retain URL-segment versioning and the established route corpus.
    /// </summary>
    [Fact]
    public void ControllersRetainVersionedRoutesAndPermissions()
    {
        string root = FindRepoRoot();
        string controllerDirectory = Path.Combine(root, "Maliev.OrderService.Api", "Controllers");
        string[] controllerFiles = Directory.GetFiles(controllerDirectory, "*Controller.cs");

        Assert.NotEmpty(controllerFiles);
        foreach (string controllerFile in controllerFiles)
        {
            string source = File.ReadAllText(controllerFile);
            Assert.Contains("[ApiController]", source, StringComparison.Ordinal);
            Assert.Contains("[ApiVersion(\"1.0\")]", source, StringComparison.Ordinal);
            Assert.Contains("v{version:apiVersion}", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/v1/", source, StringComparison.Ordinal);
            Assert.Contains("[RequirePermission(", source, StringComparison.Ordinal);
        }

        string geometrySource = File.ReadAllText(Path.Combine(controllerDirectory, "GeometryAnalysisController.cs"));
        Assert.Contains("[Route(\"geometryanalysis/v{version:apiVersion}/[controller]\")]", geometrySource, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.OrderService.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the OrderService repository root.");
    }
}

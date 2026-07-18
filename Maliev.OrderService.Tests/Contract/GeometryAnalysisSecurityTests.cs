namespace Maliev.OrderService.Tests.Contract;

/// <summary>
/// Source-level contract tests for geometry analysis security controls.
/// </summary>
public sealed class GeometryAnalysisSecurityTests
{
    /// <summary>
    /// Verifies geometry proxy endpoints require an order permission before forwarding to GeometryService.
    /// </summary>
    [Fact]
    public void GeometryAnalysisControllerRequiresOrderUpdatePermission()
    {
        var source = ReadRepoFile("Maliev.OrderService.Api", "Controllers", "GeometryAnalysisController.cs");

        Assert.Contains("[RequirePermission(OrderPermissions.OrdersUpdate)]", source);
        Assert.Contains("[HttpPost(\"{uploadId}/quality-check\")]", source);
        Assert.Contains("[HttpPost(\"{uploadId}/dfm/{processCode}\")]", source);
        Assert.Contains("[HttpDelete(\"{uploadId}\")]", source);
    }

    private static string ReadRepoFile(params string[] pathSegments)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine([root, .. pathSegments]));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.OrderService.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Maliev.OrderService repository root.");
    }
}

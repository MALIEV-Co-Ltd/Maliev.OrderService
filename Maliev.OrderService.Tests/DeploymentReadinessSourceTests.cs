namespace Maliev.OrderService.Tests
{

    /// <summary>
    /// Guards the deterministic shared-library package boundary used by CI and production images.
    /// </summary>
    public sealed class DeploymentReadinessSourceTests
    {
        /// <summary>
        /// Verifies package-mode builds use the reviewed ServiceDefaults and MessagingContracts releases.
        /// </summary>
        [Fact]
        public void PackageModePinsReviewedSharedLibrariesAcrossBuildBoundaries()
        {
            string root = FindRepoRoot();
            string buildProps = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
            string dockerfile = File.ReadAllText(Path.Combine(root, "Maliev.OrderService.Api", "Dockerfile"));
            string pullRequestWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "pr-validation.yml"));
            string developWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci-develop.yml"));

            Assert.Contains("<ServiceDefaultsVersion Condition=\"'$(ServiceDefaultsVersion)' == ''\">1.0.81-alpha</ServiceDefaultsVersion>", buildProps, StringComparison.Ordinal);
            Assert.Contains("<MessagingContractsVersion Condition=\"'$(MessagingContractsVersion)' == ''\">1.0.91-alpha</MessagingContractsVersion>", buildProps, StringComparison.Ordinal);
            Assert.DoesNotContain("<SharedLibraryVersion", buildProps, StringComparison.Ordinal);
            Assert.DoesNotContain("1.0.*", buildProps, StringComparison.Ordinal);

            foreach (string projectPath in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
            {
                if (projectPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string project = File.ReadAllText(projectPath);
                Assert.DoesNotContain("$(SharedLibraryVersion)", project, StringComparison.Ordinal);

                if (project.Contains("PackageReference Include=\"Maliev.Aspire.ServiceDefaults\"", StringComparison.Ordinal))
                {
                    Assert.Contains("Version=\"$(ServiceDefaultsVersion)\"", project, StringComparison.Ordinal);
                }

                if (project.Contains("PackageReference Include=\"Maliev.MessagingContracts\"", StringComparison.Ordinal))
                {
                    Assert.Contains("Version=\"$(MessagingContractsVersion)\"", project, StringComparison.Ordinal);
                }
            }

            Assert.Contains("COPY [\"Directory.Build.props\", \".\"]", dockerfile, StringComparison.Ordinal);
            Assert.Contains("/p:GITHUB_ACTIONS=true", dockerfile, StringComparison.Ordinal);
            Assert.Contains("/p:ServiceDefaultsVersion=\"1.0.81-alpha\"", dockerfile, StringComparison.Ordinal);
            Assert.Contains("/p:MessagingContractsVersion=\"1.0.91-alpha\"", dockerfile, StringComparison.Ordinal);
            Assert.Contains("--no-restore", dockerfile, StringComparison.Ordinal);
            Assert.Contains("id=nuget_username,required=true", dockerfile, StringComparison.Ordinal);
            Assert.Contains("id=nuget_password,required=true", dockerfile, StringComparison.Ordinal);

            Assert.Contains("-p:ServiceDefaultsVersion=1.0.81-alpha", pullRequestWorkflow, StringComparison.Ordinal);
            Assert.Contains("-p:MessagingContractsVersion=1.0.91-alpha", pullRequestWorkflow, StringComparison.Ordinal);
            Assert.DoesNotContain("1.0.*", pullRequestWorkflow, StringComparison.Ordinal);
            Assert.DoesNotContain("sed -i", developWorkflow, StringComparison.Ordinal);
            Assert.DoesNotContain("1.0.*", developWorkflow, StringComparison.Ordinal);
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
}

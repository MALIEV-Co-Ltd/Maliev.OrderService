using System.Text.RegularExpressions;

namespace Maliev.OrderService.Tests
{

    /// <summary>
    /// Guards the deterministic shared-library package boundary used by CI and production images.
    /// </summary>
    public sealed partial class DeploymentReadinessSourceTests
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
                if (projectPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                    projectPath.Contains($"{Path.DirectorySeparatorChar}.ci-sources{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Verifies untrusted pull requests reconstruct reviewed shared packages from immutable public
        /// source without receiving package, deployment, or GitOps credentials.
        /// </summary>
        [Fact]
        public void PullRequestValidationReconstructsExactDependenciesWithoutCredentials()
        {
            string root = FindRepoRoot();
            string workflowPath = Path.Combine(root, ".github", "workflows", "pr-validation.yml");
            string packageScriptPath = Path.Combine(root, "scripts", "prepare-order-ci-packages.sh");
            string nuGetConfigPath = Path.Combine(root, "NuGet.PRValidation.Config");
            string productionNuGetConfigPath = Path.Combine(root, "nuget.config");
            string dockerfilePath = Path.Combine(root, "Maliev.OrderService.Api", "Dockerfile");
            string dockerIgnorePath = Path.Combine(root, ".dockerignore");
            string gitIgnorePath = Path.Combine(root, ".gitignore");

            Assert.True(File.Exists(workflowPath), "Expected a pull-request validation workflow.");
            Assert.True(File.Exists(packageScriptPath), "Expected an exact dependency reconstruction script.");
            Assert.True(File.Exists(nuGetConfigPath), "Expected a credential-free NuGet configuration.");

            string workflow = File.ReadAllText(workflowPath);
            string packageScript = File.ReadAllText(packageScriptPath);
            string nuGetConfig = File.ReadAllText(nuGetConfigPath);
            string productionNuGetConfig = File.ReadAllText(productionNuGetConfigPath);
            string dockerfile = File.ReadAllText(dockerfilePath);
            string dockerIgnore = File.ReadAllText(dockerIgnorePath);
            string gitIgnore = File.ReadAllText(gitIgnorePath);

            Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
            Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("pull_request_target", workflow, StringComparison.Ordinal);
            Assert.Contains("permissions:", workflow, StringComparison.Ordinal);
            Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
            Assert.Contains("NUGET_PACKAGES: ${{ github.workspace }}/.ci-nuget/packages", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("packages: read", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("secrets:", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("GITOPS_PAT", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("github.token", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NUGET_USERNAME", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NUGET_PASSWORD", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("concurrency:", workflow, StringComparison.Ordinal);
            Assert.Contains("cancel-in-progress: true", workflow, StringComparison.Ordinal);

            Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.MessagingContracts", workflow, StringComparison.Ordinal);
            Assert.Contains("ref: 0bcd4c704d842211c5ff9bd6b9c4b3aacfcbd8e7", workflow, StringComparison.Ordinal);
            Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.Aspire", workflow, StringComparison.Ordinal);
            Assert.Contains("ref: 7121d57705fc1eff6c7ebb6a69e33e9c26ebfccc", workflow, StringComparison.Ordinal);
            Assert.Contains("prepare-order-ci-packages.sh", workflow, StringComparison.Ordinal);
            Assert.Contains("order-ci-packages", workflow, StringComparison.Ordinal);
            Assert.Contains("include-hidden-files: true", workflow, StringComparison.Ordinal);
            Assert.Contains("overwrite: true", workflow, StringComparison.Ordinal);
            Assert.Contains("artifact-digest", workflow, StringComparison.Ordinal);
            Assert.Contains("[[ \"$ARTIFACT_DIGEST\" =~ ^[0-9a-f]{64}$ ]]", workflow, StringComparison.Ordinal);
            Assert.Contains("sha256sum --check SHA256SUMS.txt", workflow, StringComparison.Ordinal);

            Assert.Contains("dotnet restore Maliev.OrderService.slnx", workflow, StringComparison.Ordinal);
            Assert.Contains("--configfile NuGet.PRValidation.Config", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet build Maliev.OrderService.slnx --configuration Release --no-restore", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet test Maliev.OrderService.slnx --configuration Release --no-build", workflow, StringComparison.Ordinal);
            Assert.Contains("-p:ServiceDefaultsVersion=1.0.81-alpha", workflow, StringComparison.Ordinal);
            Assert.Contains("-p:MessagingContractsVersion=1.0.91-alpha", workflow, StringComparison.Ordinal);
            Assert.Contains("dependency_restore_stage=restore-local", workflow, StringComparison.Ordinal);
            Assert.Contains("push: false", workflow, StringComparison.Ordinal);
            Assert.Contains("Smoke test production image", workflow, StringComparison.Ordinal);
            Assert.Contains("/order/liveness", workflow, StringComparison.Ordinal);
            Assert.Contains("postgres:18-alpine", workflow, StringComparison.Ordinal);
            Assert.Contains("CORS__AllowedOrigins__0=http://localhost", workflow, StringComparison.Ordinal);
            Assert.Contains("severity: HIGH,CRITICAL", workflow, StringComparison.Ordinal);
            Assert.Contains("exit-code: \"1\"", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("argocd", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("kubectl", workflow, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("readonly messaging_commit=\"0bcd4c704d842211c5ff9bd6b9c4b3aacfcbd8e7\"", packageScript, StringComparison.Ordinal);
            Assert.Contains("readonly aspire_commit=\"7121d57705fc1eff6c7ebb6a69e33e9c26ebfccc\"", packageScript, StringComparison.Ordinal);
            Assert.Contains("readonly messaging_version=\"1.0.91-alpha\"", packageScript, StringComparison.Ordinal);
            Assert.Contains("readonly service_defaults_version=\"1.0.81-alpha\"", packageScript, StringComparison.Ordinal);
            Assert.Contains("dotnet restore \"$generator_project\" --configfile \"$ci_nuget_config\"", packageScript, StringComparison.Ordinal);
            Assert.Contains("dotnet run --project tools/Generator/Generator.csproj --configuration Release --no-restore", packageScript, StringComparison.Ordinal);
            Assert.Contains("printf 'root = true", packageScript, StringComparison.Ordinal);
            Assert.Contains("SHA256SUMS.txt", packageScript, StringComparison.Ordinal);
            Assert.DoesNotContain("--source", packageScript, StringComparison.Ordinal);

            Assert.DoesNotContain("nuget.pkg.github.com", nuGetConfig, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("packageSourceCredentials", nuGetConfig, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<add key=\"maliev-ci\" value=\".ci-packages\" />", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<packageSource key=\"nuget.org\">", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<package pattern=\"*\" />", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<packageSource key=\"maliev-ci\">", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<package pattern=\"Maliev.*\" />", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<packageSourceMapping>", productionNuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<packageSource key=\"nuget.org\">", productionNuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<packageSource key=\"github\">", productionNuGetConfig, StringComparison.Ordinal);
            Assert.Contains("<package pattern=\"Maliev.*\" />", productionNuGetConfig, StringComparison.Ordinal);

            Assert.Contains("ARG dependency_restore_stage=restore-private", dockerfile, StringComparison.Ordinal);
            Assert.Contains("FROM ${dependency_restore_stage} AS build", dockerfile, StringComparison.Ordinal);
            Assert.Contains("FROM build-base AS restore-local", dockerfile, StringComparison.Ordinal);
            Assert.Contains("--configfile \"NuGet.PRValidation.Config\"", dockerfile, StringComparison.Ordinal);
            Assert.Contains("COPY [\".ci-packages/\", \".ci-packages/\"]", dockerfile, StringComparison.Ordinal);
            Assert.DoesNotContain("HEALTHCHECK", dockerfile, StringComparison.Ordinal);
            Assert.Contains("!.ci-packages/*.nupkg", dockerIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-sources/", dockerIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-nuget/", dockerIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-packages/*.nupkg", gitIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-packages/*.snupkg", gitIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-packages/SHA256SUMS.txt", gitIgnore, StringComparison.Ordinal);
            Assert.Contains(".ci-nuget/", gitIgnore, StringComparison.Ordinal);

            MatchCollection unpinnedActions = UnpinnedActionRegex().Matches(workflow);
            Assert.Empty(unpinnedActions.Select(match => match.Value));
        }

        [GeneratedRegex(@"uses:\s+[^\s@]+@(?![0-9a-f]{40}(?:\s|$))[^\s]+", RegexOptions.CultureInvariant)]
        private static partial Regex UnpinnedActionRegex();

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

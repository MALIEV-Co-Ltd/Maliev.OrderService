using System.Text.RegularExpressions;

namespace Maliev.OrderService.Tests;

/// <summary>
/// Guards the credential-free validation boundary used for pull requests and protected branches.
/// </summary>
public sealed partial class DeploymentReadinessSourceTests
{
    private const string AspireCommit = "7121d57705fc1eff6c7ebb6a69e33e9c26ebfccc";
    private const string MessagingContractsCommit = "0bcd4c704d842211c5ff9bd6b9c4b3aacfcbd8e7";

    /// <summary>
    /// Verifies every active workflow is validation-only and cannot publish or deploy artifacts.
    /// </summary>
    [Fact]
    public void WorkflowsAreReadOnlyValidationOnly()
    {
        string root = FindRepoRoot();
        string workflowDirectory = Path.Combine(root, ".github", "workflows");
        string[] expectedWorkflows =
        [
            "_validate.yml",
            "ci-develop.yml",
            "ci-main.yml",
            "ci-staging.yml",
            "pr-validation.yml"
        ];

        Assert.Equal(
            expectedWorkflows,
            Directory.GetFiles(workflowDirectory, "*.yml")
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal)
                .ToArray());

        foreach (string workflowPath in Directory.GetFiles(workflowDirectory, "*.yml"))
        {
            string workflow = File.ReadAllText(workflowPath);
            Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("pull_request_target", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("secrets.", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("credentials_json", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("docker push", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("build-push-action", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gcloud", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gitops", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("kustomize", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gh pr", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("contents: write", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("packages: write", workflow, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("id-token: write", workflow, StringComparison.OrdinalIgnoreCase);

            MatchCollection unpinnedActions = UnpinnedActionRegex().Matches(workflow);
            Assert.Empty(unpinnedActions.Select(match => match.Value));
        }
    }

    /// <summary>
    /// Verifies CI restores immutable public shared source without package credentials.
    /// </summary>
    [Fact]
    public void ValidationUsesImmutablePublicSharedSource()
    {
        string root = FindRepoRoot();
        string validationWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "_validate.yml"));
        string validationNuGetConfig = File.ReadAllText(Path.Combine(root, "nuget.validation.config"));

        Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.Aspire", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains($"ref: {AspireCommit}", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.MessagingContracts", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains($"ref: {MessagingContractsCommit}", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("/p:SharedSourceRoot=${{ github.workspace }}/shared", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("--configfile nuget.validation.config", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("dotnet build Maliev.OrderService.slnx", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test Maliev.OrderService.slnx", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("dotnet format Maliev.OrderService.slnx whitespace", validationWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet format whitespace Maliev.OrderService.slnx", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("SharedSourceRoot: ${{ github.workspace }}/shared", validationWorkflow, StringComparison.Ordinal);
        Assert.Contains("package --vulnerable --include-transitive", validationWorkflow, StringComparison.Ordinal);
        Assert.Equal(3, validationWorkflow.Split("persist-credentials: false", StringSplitOptions.None).Length - 1);

        string sharedEditorConfig = File.ReadAllText(Path.Combine(root, "shared", ".editorconfig"));
        Assert.Contains("root = true", sharedEditorConfig, StringComparison.Ordinal);

        Assert.Contains("https://api.nuget.org/v3/index.json", validationNuGetConfig, StringComparison.Ordinal);
        Assert.DoesNotContain("nuget.pkg.github.com", validationNuGetConfig, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("packageSourceCredentials", validationNuGetConfig, StringComparison.OrdinalIgnoreCase);

        foreach (string projectPath in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            if (!IsOwnedProject(root, projectPath))
            {
                continue;
            }

            string project = File.ReadAllText(projectPath);
            if (project.Contains("Maliev.Aspire.ServiceDefaults", StringComparison.Ordinal))
            {
                Assert.Contains("$(SharedSourceRoot)/Maliev.Aspire/", project, StringComparison.Ordinal);
            }

            if (project.Contains("Maliev.MessagingContracts", StringComparison.Ordinal))
            {
                Assert.Contains("$(SharedSourceRoot)/Maliev.MessagingContracts/", project, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Verifies external and generated project paths are excluded on both runner path conventions.
    /// </summary>
    [Theory]
    [InlineData("Maliev.OrderService.Api/Maliev.OrderService.Api.csproj", true)]
    [InlineData("Maliev.OrderService.Api\\Maliev.OrderService.Api.csproj", true)]
    [InlineData("shared/Maliev.Aspire/Maliev.Aspire.ServiceDefaults/Maliev.Aspire.ServiceDefaults.csproj", false)]
    [InlineData("shared\\Maliev.MessagingContracts\\generated\\csharp\\Maliev.MessagingContracts.csproj", false)]
    [InlineData("Maliev.OrderService.Api/obj/Generated.csproj", false)]
    public void SharedSourceOwnershipIsPlatformIndependent(string relativePath, bool expected)
    {
        Assert.Equal(expected, IsOwnedRelativeProject(relativePath));
    }

    /// <summary>
    /// Verifies image packaging consumes a prevalidated publish artifact without dependency credentials.
    /// </summary>
    [Fact]
    public void RuntimeImageIsCredentialFreeAndTraceable()
    {
        string root = FindRepoRoot();
        string dockerfile = File.ReadAllText(Path.Combine(root, "Maliev.OrderService.Api", "Dockerfile"));

        Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:10.0", dockerfile, StringComparison.Ordinal);
        Assert.Contains("COPY --chown=app:app publish/ .", dockerfile, StringComparison.Ordinal);
        Assert.Contains("USER app", dockerfile, StringComparison.Ordinal);
        Assert.Contains("org.opencontainers.image.revision", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet restore", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nuget_password", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--mount=type=secret", dockerfile, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"uses:\s+[^\s@]+@(?![0-9a-f]{40}(?:\s|$))[^\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex UnpinnedActionRegex();

    private static bool IsOwnedProject(string root, string projectPath)
    {
        return IsOwnedRelativeProject(Path.GetRelativePath(root, projectPath));
    }

    private static bool IsOwnedRelativeProject(string relativePath)
    {
        string normalizedPath = relativePath.Replace('\\', '/');
        return !normalizedPath.StartsWith("shared/", StringComparison.OrdinalIgnoreCase)
            && !normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
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

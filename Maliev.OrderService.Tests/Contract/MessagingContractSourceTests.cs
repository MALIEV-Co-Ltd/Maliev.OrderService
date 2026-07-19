using System.Text.RegularExpressions;

namespace Maliev.OrderService.Tests.Contract;

/// <summary>
/// Guards centralized order messaging ownership.
/// </summary>
public sealed partial class MessagingContractSourceTests
{
    /// <summary>
    /// Verifies OrderService consumes and publishes generated shared contracts without local event definitions.
    /// </summary>
    [Fact]
    public void MessagingUsesCentralGeneratedContracts()
    {
        string root = FindRepoRoot();
        string apiDirectory = Path.Combine(root, "Maliev.OrderService.Api");
        string[] sourceFiles = Directory.GetFiles(apiDirectory, "*.cs", SearchOption.AllDirectories);
        string source = string.Join(Environment.NewLine, sourceFiles.Select(File.ReadAllText));

        Assert.DoesNotMatch(LocalEventDefinitionRegex(), source);
        Assert.Contains("using Maliev.MessagingContracts.Contracts.Orders;", source, StringComparison.Ordinal);
        Assert.Contains("IConsumer<PaymentCompletedEvent>", source, StringComparison.Ordinal);
        Assert.Contains("IConsumer<JobStatusChangedEvent>", source, StringComparison.Ordinal);
        Assert.Contains("IConsumer<FileDeletedEvent>", source, StringComparison.Ordinal);
        Assert.Contains("new OrderCreatedEvent(", source, StringComparison.Ordinal);
        Assert.Contains("new OrderStatusChangedEvent(", source, StringComparison.Ordinal);
        Assert.Contains("new OrderPaidEvent(", source, StringComparison.Ordinal);
        Assert.Contains("new OrderCompletedEvent(", source, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\b(?:class|record)\s+\w+Event\b", RegexOptions.CultureInvariant)]
    private static partial Regex LocalEventDefinitionRegex();

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

namespace Maliev.OrderService.Tests.Unit.Configuration;

public sealed class OrderPaymentOutcomeEndpointTests
{
    [Fact]
    public void ProgramConfiguresRetryForPaymentOutcomeEndpoint()
    {
        var programPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Maliev.OrderService.Api",
            "Program.cs"));
        var source = File.ReadAllText(programPath);

        Assert.Contains("ReceiveEndpoint(\"order-payment-outcomes\"", source, StringComparison.Ordinal);
        Assert.Contains("UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(2)))", source, StringComparison.Ordinal);
    }
}

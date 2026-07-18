namespace Maliev.OrderService.Tests.Unit.Services
{
    public class OrderManagementOutboxOrderTests
    {
        [Fact]
        public void CreateOrderAsyncPublishesOrderCreatedBeforeSavingForOutbox()
        {
            string source = File.ReadAllText(FindOrderManagementServiceSourcePath());
            string methodBody = ExtractMethodSource(
                source,
                "public async Task<OrderResponse> CreateOrderAsync");

            AssertCallAppearsBeforeSaveChanges(
                methodBody,
                "await _publishEndpoint.Publish(new OrderCreatedEvent(",
                "CreateOrderAsync must publish OrderCreatedEvent before SaveChangesAsync so the EF bus outbox persists the event atomically with the order.");
        }

        [Fact]
        public void UpdateOutsourcingAsyncPublishesOutsourcingChangedBeforeSavingForOutbox()
        {
            string source = File.ReadAllText(FindOrderManagementServiceSourcePath());
            string methodBody = ExtractMethodSource(
                source,
                "public async Task<OrderResponse> UpdateOutsourcingAsync");

            AssertCallAppearsBeforeSaveChanges(
                methodBody,
                "await _publishEndpoint.Publish(new OrderOutsourcingChangedEvent(",
                "UpdateOutsourcingAsync must publish OrderOutsourcingChangedEvent before SaveChangesAsync so the EF bus outbox persists the event atomically with outsourcing state.");
        }

        private static string FindOrderManagementServiceSourcePath()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(
                    directory.FullName,
                    "Maliev.OrderService.Api",
                    "Services",
                    "Business",
                    "OrderManagementService.cs");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not locate Maliev.OrderService.Api/Services/Business/OrderManagementService.cs");
        }

        private static string ExtractMethodSource(string source, string methodSignature)
        {
            int methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.True(methodStart >= 0, $"Could not find {methodSignature} source.");

            int openingBrace = source.IndexOf('{', methodStart);
            Assert.True(openingBrace > methodStart, $"Could not find opening brace for {methodSignature}.");

            int depth = 0;
            for (int index = openingBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source[methodStart..(index + 1)];
                    }
                }
            }

            throw new InvalidOperationException($"Could not isolate {methodSignature} source.");
        }

        private static void AssertCallAppearsBeforeSaveChanges(
            string methodBody,
            string expectedCall,
            string failureMessage)
        {
            int callIndex = methodBody.IndexOf(expectedCall, StringComparison.Ordinal);
            int saveIndex = methodBody.IndexOf(
                "_ = await _context.SaveChangesAsync(cancellationToken);",
                StringComparison.Ordinal);

            Assert.True(callIndex >= 0, $"Expected call not found: {expectedCall}");
            Assert.True(saveIndex >= 0, "Expected SaveChangesAsync call not found.");
            Assert.True(callIndex < saveIndex, failureMessage);
        }
    }
}

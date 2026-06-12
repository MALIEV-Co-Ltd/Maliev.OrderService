using System.Text.Json;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Tests.Unit.Dto
{
    public class OrderLineItemResponseTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public void OrderLineItemResponseSerializesCustomerContextWireShape()
        {
            string materialSnapshotJson = JsonSerializer.Serialize(new { materialCode = "pla-black" }, _jsonOptions);
            string configurationSnapshotJson = JsonSerializer.Serialize(new { fileName = "gear.step" }, _jsonOptions);
            var response = new OrderLineItemResponse
            {
                OrderItemId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                SourceProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                SourceProjectPartId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CustomerId = "CUST-PROD-001",
                CustomerName = "Production Buyer Ltd.",
                MaterialId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                MaterialSnapshotJson = materialSnapshotJson,
                ConfigurationSnapshotJson = configurationSnapshotJson,
                Technology = "FDM",
                VolumeCm3 = 12.5m,
                Quantity = 2,
                EstimatedPrintTimeMinutes = 40,
                DeliveryDate = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc)
            };

            string json = JsonSerializer.Serialize(response, _jsonOptions);
            using var document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            Assert.Equal("CUST-PROD-001", root.GetProperty("customerId").GetString());
            Assert.Equal("Production Buyer Ltd.", root.GetProperty("customerName").GetString());
            Assert.Equal("FDM", root.GetProperty("technology").GetString());
        }
    }
}

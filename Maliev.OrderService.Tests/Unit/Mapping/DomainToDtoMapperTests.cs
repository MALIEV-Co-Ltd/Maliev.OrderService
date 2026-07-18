using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Domain.Entities;
using Xunit;

namespace Maliev.OrderService.Tests.Unit.Mapping;

public class DomainToDtoMapperTests
{
    [Fact]
    public void ToOrderResponseMapsAllFields()
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = "CUST-1",
            CustomerType = "Business",
            ServiceCategoryId = 1,
            ProcessTypeId = 2,
            Requirements = "Test requirements",
            OrderedQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user1"
        };

        var response = order.ToOrderResponse();

        Assert.Equal(order.OrderId, response.OrderId);
        Assert.Equal(order.CustomerId, response.CustomerId);
        Assert.Equal(order.CustomerType, response.CustomerType);
        Assert.Equal(order.Requirements, response.Requirements);
    }

    [Fact]
    public void ToOrderMapsRequestToModel()
    {
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-2",
            CustomerType = "Individual",
            ServiceCategoryId = 3,
            ProcessTypeId = 4,
            Requirements = "More requirements"
        };

        var order = request.ToOrder();

        Assert.Equal(request.CustomerId, order.CustomerId);
        Assert.Equal(request.CustomerType, order.CustomerType);
        Assert.Equal(request.Requirements, order.Requirements);
    }

    [Fact]
    public void UpdateOrderUpdatesSpecificFields()
    {
        var order = new Order { Requirements = "Old" };
        var request = new UpdateOrderRequest
        {
            Version = "AAAA",
            Requirements = "New",
            OrderedQuantity = 50
        };

        order.UpdateOrder(request);

        Assert.Equal("New", order.Requirements);
        Assert.Equal(50, order.OrderedQuantity);
    }

    [Fact]
    public void ToOrderResponseWithAttributesMapsAttributes()
    {
        var order = new Order
        {
            OrderId = "ORD-1",
            CustomerId = "CUST-1",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            PrintingAttributes = new Order3DPrintingAttributes { ThreadTapRequired = true, InsertRequired = false },
            CncAttributes = new OrderCncMachiningAttributes { TapRequired = true, Tolerance = "0.1mm" },
            SheetMetalAttributes = new OrderSheetMetalAttributes { Thickness = "2.0mm", WeldingRequired = true },
            ScanningAttributes = new Order3DScanningAttributes { RequiredAccuracy = "High" },
            DesignAttributes = new Order3DDesignAttributes { ComplexityLevel = "High" }
        };

        var response = order.ToOrderResponse();

        Assert.NotNull(response.PrintingAttributes);
        Assert.True(response.PrintingAttributes.ThreadTapRequired);
        Assert.NotNull(response.CncAttributes);
        Assert.True(response.CncAttributes.TapRequired);
        Assert.NotNull(response.SheetMetalAttributes);
        Assert.True(response.SheetMetalAttributes.WeldingRequired);
        Assert.NotNull(response.ScanningAttributes);
        Assert.Equal("High", response.ScanningAttributes.RequiredAccuracy);
        Assert.NotNull(response.DesignAttributes);
        Assert.Equal("High", response.DesignAttributes.ComplexityLevel);
    }

    [Fact]
    public void OrderStatusMappingsWork()
    {
        var request = new CreateOrderStatusRequest { Status = "Approved", InternalNotes = "Notes" };
        var model = request.ToOrderStatus();
        Assert.Equal("Approved", model.Status);

        var status = new OrderStatus { StatusId = 1, OrderId = "O1", Status = "Paid", UpdatedBy = "System", Timestamp = DateTime.UtcNow };
        var response = status.ToOrderStatusResponse();
        Assert.Equal(status.Status, response.Status);
    }

    [Fact]
    public void OrderFileMappingsWork()
    {
        var request = new UploadOrderFileRequest { FileRole = "Input", FileCategory = "CAD" };
        var model = request.ToOrderFile();
        Assert.Equal("Input", model.FileRole);

        var file = new OrderFile { FileId = 1, OrderId = "O1", FileName = "f.txt", FileRole = "Output", FileCategory = "Drawing", ObjectPath = "p/f.txt", FileType = "text/plain" };
        var response = file.ToOrderFileResponse();
        Assert.Equal(file.FileName, response.FileName);
    }

    [Fact]
    public void OrderNoteMappingsWork()
    {
        var request = new CreateOrderNoteRequest { NoteType = "internal", NoteText = "Secret" };
        var model = request.ToOrderNote();
        Assert.Equal("internal", model.NoteType);

        var note = new OrderNote { NoteId = 1, OrderId = "O1", NoteType = "customer", NoteText = "Public", CreatedBy = "User", CreatedAt = DateTime.UtcNow };
        var response = note.ToOrderNoteResponse();
        Assert.Equal(note.NoteText, response.NoteText);
    }
}

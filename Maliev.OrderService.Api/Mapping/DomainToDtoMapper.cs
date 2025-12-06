using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Api.Mapping;

/// <summary>
/// Pure .NET mapper for order-related domain models and DTOs
/// </summary>
public static class DomainToDtoMapper
{
    // Order mappings
    /// <summary>Maps an Order domain model to an OrderResponse DTO</summary>
    public static OrderResponse ToOrderResponse(this Order order)
    {
        return new OrderResponse
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            CustomerType = order.CustomerType,
            ServiceCategoryId = order.ServiceCategoryId,
            ServiceCategoryName = order.ServiceCategory?.Name,
            ProcessTypeId = order.ProcessTypeId,
            ProcessTypeName = order.ProcessType?.Name,
            IsConfidential = order.IsConfidential,
            MaterialId = order.MaterialId,
            MaterialName = order.MaterialName,
            ColorId = order.ColorId,
            ColorName = order.ColorName,
            SurfaceFinishingId = order.SurfaceFinishingId,
            SurfaceFinishingName = order.SurfaceFinishingName,
            MaterialCacheUpdatedAt = order.MaterialCacheUpdatedAt,
            OrderedQuantity = order.OrderedQuantity,
            ManufacturedQuantity = order.ManufacturedQuantity,
            LeadTimeDays = order.LeadTimeDays,
            PromisedDeliveryDate = order.PromisedDeliveryDate,
            ActualDeliveryDate = order.ActualDeliveryDate,
            QuotedAmount = order.QuotedAmount,
            QuoteCurrency = order.QuoteCurrency,
            PaymentId = order.PaymentId,
            PaymentStatus = order.PaymentStatus,
            AssignedEmployeeId = order.AssignedEmployeeId,
            DepartmentId = order.DepartmentId,
            Requirements = order.Requirements,
            CurrentStatus = order.OrderStatuses.OrderByDescending(s => s.Timestamp).FirstOrDefault()?.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CreatedBy = order.CreatedBy,
            UpdatedBy = order.UpdatedBy,
            Version = Convert.ToBase64String(order.Version),
            PrintingAttributes = order.PrintingAttributes?.ToPrintingAttributesDto(),
            CncAttributes = order.CncAttributes?.ToCncAttributesDto(),
            SheetMetalAttributes = order.SheetMetalAttributes?.ToSheetMetalAttributesDto(),
            ScanningAttributes = order.ScanningAttributes?.ToScanningAttributesDto(),
            DesignAttributes = order.DesignAttributes?.ToDesignAttributesDto()
        };
    }

    /// <summary>Maps a CreateOrderRequest DTO to an Order domain model</summary>
    public static Order ToOrder(this CreateOrderRequest request)
    {
        return new Order
        {
            CustomerId = request.CustomerId,
            CustomerType = request.CustomerType,
            ServiceCategoryId = request.ServiceCategoryId,
            ProcessTypeId = request.ProcessTypeId,
            Requirements = request.Requirements,
            IsConfidential = request.IsConfidential,
            OrderedQuantity = request.OrderedQuantity,
            MaterialId = request.MaterialId,
            ColorId = request.ColorId,
            SurfaceFinishingId = request.SurfaceFinishingId,
            LeadTimeDays = request.LeadTimeDays,
            PromisedDeliveryDate = request.PromisedDeliveryDate,
            AssignedEmployeeId = request.AssignedEmployeeId,
            DepartmentId = request.DepartmentId
        };
    }

    /// <summary>Updates an Order domain model from an UpdateOrderRequest DTO</summary>
    public static void UpdateOrder(this Order order, UpdateOrderRequest request)
    {
        // Only update properties that are present in UpdateOrderRequest
        if (request.Requirements != null)
            order.Requirements = request.Requirements;
        if (request.OrderedQuantity.HasValue)
            order.OrderedQuantity = request.OrderedQuantity;
        if (request.ManufacturedQuantity.HasValue)
            order.ManufacturedQuantity = request.ManufacturedQuantity;
        if (request.MaterialId.HasValue)
            order.MaterialId = request.MaterialId;
        if (request.ColorId.HasValue)
            order.ColorId = request.ColorId;
        if (request.SurfaceFinishingId.HasValue)
            order.SurfaceFinishingId = request.SurfaceFinishingId;
        if (request.LeadTimeDays.HasValue)
            order.LeadTimeDays = request.LeadTimeDays;
        if (request.PromisedDeliveryDate.HasValue)
            order.PromisedDeliveryDate = request.PromisedDeliveryDate;
        if (request.ActualDeliveryDate.HasValue)
            order.ActualDeliveryDate = request.ActualDeliveryDate;
        if (request.QuotedAmount.HasValue)
            order.QuotedAmount = request.QuotedAmount;
        if (request.QuoteCurrency != null)
            order.QuoteCurrency = request.QuoteCurrency;
        if (request.AssignedEmployeeId != null)
            order.AssignedEmployeeId = request.AssignedEmployeeId;
        if (request.DepartmentId != null)
            order.DepartmentId = request.DepartmentId;
    }

    // Service-specific attributes mappings
    /// <summary>Maps Order3DPrintingAttributes to Order3DPrintingAttributesDto</summary>
    public static Order3DPrintingAttributesDto ToPrintingAttributesDto(this Order3DPrintingAttributes attributes)
    {
        return new Order3DPrintingAttributesDto
        {
            InfillPercentage = null, // Not in domain model
            LayerHeight = null, // Not in domain model
            SupportType = null, // Not in domain model
            Color = null, // Not in domain model
            Finish = null // Not in domain model
        };
    }

    /// <summary>Maps OrderCncMachiningAttributes to OrderCncMachiningAttributesDto</summary>
    public static OrderCncMachiningAttributesDto ToCncAttributesDto(this OrderCncMachiningAttributes attributes)
    {
        return new OrderCncMachiningAttributesDto
        {
            Tolerance = attributes.Tolerance,
            SurfaceFinish = attributes.SurfaceRoughness,
            ThreadType = null // Not in domain model
        };
    }

    /// <summary>Maps OrderSheetMetalAttributes to OrderSheetMetalAttributesDto</summary>
    public static OrderSheetMetalAttributesDto ToSheetMetalAttributesDto(this OrderSheetMetalAttributes attributes)
    {
        return new OrderSheetMetalAttributesDto
        {
            SheetThickness = attributes.Thickness,
            BendingMethod = null, // Not in domain model
            WeldingType = attributes.WeldingRequired ? attributes.WeldingDetails : null,
            CoatingType = null // Not in domain model
        };
    }

    /// <summary>Maps Order3DScanningAttributes to Order3DScanningAttributesDto</summary>
    public static Order3DScanningAttributesDto ToScanningAttributesDto(this Order3DScanningAttributes attributes)
    {
        return new Order3DScanningAttributesDto
        {
            ScanResolution = attributes.RequiredAccuracy,
            ScanFormat = attributes.OutputFileFormats,
            ObjectSize = null // Not in domain model
        };
    }

    /// <summary>Maps Order3DDesignAttributes to Order3DDesignAttributesDto</summary>
    public static Order3DDesignAttributesDto ToDesignAttributesDto(this Order3DDesignAttributes attributes)
    {
        return new Order3DDesignAttributesDto
        {
            DesignSoftware = attributes.DesignSoftware,
            FileFormat = null, // Not in domain model
            DesignComplexity = attributes.ComplexityLevel
        };
    }

    // OrderStatus mappings
    /// <summary>Maps an OrderStatus domain model to an OrderStatusResponse DTO</summary>
    public static OrderStatusResponse ToOrderStatusResponse(this OrderStatus status)
    {
        return new OrderStatusResponse
        {
            StatusId = status.StatusId,
            OrderId = status.OrderId,
            Status = status.Status,
            InternalNotes = status.InternalNotes,
            CustomerNotes = status.CustomerNotes,
            UpdatedBy = status.UpdatedBy,
            Timestamp = status.Timestamp
        };
    }

    /// <summary>Maps a CreateOrderStatusRequest DTO to an OrderStatus domain model</summary>
    public static OrderStatus ToOrderStatus(this CreateOrderStatusRequest request)
    {
        return new OrderStatus
        {
            Status = request.Status,
            InternalNotes = request.InternalNotes,
            CustomerNotes = request.CustomerNotes
        };
    }

    // OrderFile mappings
    /// <summary>Maps an OrderFile domain model to an OrderFileResponse DTO</summary>
    public static OrderFileResponse ToOrderFileResponse(this OrderFile file)
    {
        return new OrderFileResponse
        {
            FileId = file.FileId,
            OrderId = file.OrderId,
            FileName = file.FileName,
            FileRole = file.FileRole,
            FileCategory = file.FileCategory,
            FileSize = file.FileSize,
            FileType = file.FileType,
            ObjectPath = file.ObjectPath,
            AccessLevel = file.AccessLevel,
            DesignUnits = file.DesignUnits,
            UploadedBy = file.UploadedBy,
            UploadedAt = file.UploadedAt,
            DeletedAt = file.DeletedAt
        };
    }

    /// <summary>Maps an UploadOrderFileRequest DTO to an OrderFile domain model</summary>
    public static OrderFile ToOrderFile(this UploadOrderFileRequest request)
    {
        return new OrderFile
        {
            FileRole = request.FileRole,
            FileCategory = request.FileCategory,
            DesignUnits = request.DesignUnits
        };
    }

    // OrderNote mappings
    /// <summary>Maps an OrderNote domain model to an OrderNoteResponse DTO</summary>
    public static OrderNoteResponse ToOrderNoteResponse(this OrderNote note)
    {
        return new OrderNoteResponse
        {
            NoteId = note.NoteId,
            OrderId = note.OrderId,
            NoteType = note.NoteType,
            NoteText = note.NoteText,
            CreatedBy = note.CreatedBy,
            CreatedAt = note.CreatedAt
        };
    }

    /// <summary>Maps a CreateOrderNoteRequest DTO to an OrderNote domain model</summary>
    public static OrderNote ToOrderNote(this CreateOrderNoteRequest request)
    {
        return new OrderNote
        {
            NoteType = request.NoteType,
            NoteText = request.NoteText
        };
    }
}

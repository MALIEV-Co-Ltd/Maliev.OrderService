using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Domain.Entities;
using System.Globalization;
using System.Text.Json;

namespace Maliev.OrderService.Api.Mapping
{
    /// <summary>
    /// Pure .NET mapper for order-related domain models and DTOs
    /// </summary>
    public static class DomainToDtoMapper
    {
        private static readonly JsonSerializerOptions _productionItemJsonOptions = new(JsonSerializerDefaults.Web);

        // Order mappings
        /// <summary>Maps an Order domain model to an OrderResponse DTO</summary>
        public static OrderResponse ToOrderResponse(this Order order, uint? xmin = null)
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
                CustomerPoNumber = order.CustomerPoNumber,
                CustomerPoFileId = order.CustomerPoFileId,
                BillingAddressId = order.BillingAddressId,
                ShippingAddressId = order.ShippingAddressId,
                ShippingAddressLine1 = order.ShippingAddressLine1,
                ShippingAddressLine2 = order.ShippingAddressLine2,
                ShippingCity = order.ShippingCity,
                ShippingProvince = order.ShippingProvince,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                BillingCompanyName = order.BillingCompanyName,
                BillingVatNumber = order.BillingVatNumber,
                DeliveryContactName = order.DeliveryContactName,
                DeliveryContactPhone = order.DeliveryContactPhone,
                DeliveryContactEmail = order.DeliveryContactEmail,
                PaymentId = order.PaymentId,
                PaymentStatus = order.PaymentStatus,
                AssignedEmployeeId = order.AssignedEmployeeId,
                DepartmentId = order.DepartmentId,
                Requirements = order.Requirements,
                IsOutsourced = order.IsOutsourced,
                SupplierCostTHB = order.SupplierCostTHB,
                SupplierName = order.SupplierName,
                SupplierEstimatedDelivery = order.SupplierEstimatedDelivery,
                CurrentStatus = order.OrderStatuses.OrderByDescending(s => s.Timestamp).FirstOrDefault()?.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CreatedBy = order.CreatedBy,
                UpdatedBy = order.UpdatedBy,
                Version = xmin?.ToString(CultureInfo.InvariantCulture) ?? "0",
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
                ProductionItemsJson = request.ProductionItems.Count > 0
                    ? JsonSerializer.Serialize(request.ProductionItems, _productionItemJsonOptions)
                    : null,
                IsConfidential = request.IsConfidential,
                OrderedQuantity = request.OrderedQuantity,
                MaterialId = request.MaterialId,
                ColorId = request.ColorId,
                SurfaceFinishingId = request.SurfaceFinishingId,
                LeadTimeDays = request.LeadTimeDays,
                PromisedDeliveryDate = request.PromisedDeliveryDate,
                AssignedEmployeeId = request.AssignedEmployeeId,
                DepartmentId = request.DepartmentId,
                CustomerPoNumber = request.CustomerPoNumber,
                CustomerPoFileId = request.CustomerPoFileId,
                BillingAddressId = request.BillingAddressId,
                ShippingAddressId = request.ShippingAddressId,
                ShippingAddressLine1 = request.ShippingAddressLine1,
                ShippingAddressLine2 = request.ShippingAddressLine2,
                ShippingCity = request.ShippingCity,
                ShippingProvince = request.ShippingProvince,
                ShippingPostalCode = request.ShippingPostalCode,
                ShippingCountry = request.ShippingCountry,
                BillingCompanyName = request.BillingCompanyName,
                BillingVatNumber = request.BillingVatNumber,
                DeliveryContactName = request.DeliveryContactName,
                DeliveryContactPhone = request.DeliveryContactPhone,
                DeliveryContactEmail = request.DeliveryContactEmail
            };
        }

        /// <summary>Updates an Order domain model from an UpdateOrderRequest DTO</summary>
        public static void UpdateOrder(this Order order, UpdateOrderRequest request)
        {
            // Only update properties that are present in UpdateOrderRequest
            if (request.Requirements != null)
            {
                order.Requirements = request.Requirements;
            }

            if (request.OrderedQuantity.HasValue)
            {
                order.OrderedQuantity = request.OrderedQuantity;
            }

            if (request.ManufacturedQuantity.HasValue)
            {
                order.ManufacturedQuantity = request.ManufacturedQuantity;
            }

            if (request.MaterialId.HasValue)
            {
                order.MaterialId = request.MaterialId;
            }

            if (request.ColorId.HasValue)
            {
                order.ColorId = request.ColorId;
            }

            if (request.SurfaceFinishingId.HasValue)
            {
                order.SurfaceFinishingId = request.SurfaceFinishingId;
            }

            if (request.LeadTimeDays.HasValue)
            {
                order.LeadTimeDays = request.LeadTimeDays;
            }

            if (request.PromisedDeliveryDate.HasValue)
            {
                order.PromisedDeliveryDate = request.PromisedDeliveryDate;
            }

            if (request.ActualDeliveryDate.HasValue)
            {
                order.ActualDeliveryDate = request.ActualDeliveryDate;
            }

            if (request.QuotedAmount.HasValue)
            {
                order.QuotedAmount = request.QuotedAmount;
            }

            if (request.QuoteCurrency != null)
            {
                order.QuoteCurrency = request.QuoteCurrency;
            }

            if (request.AssignedEmployeeId != null)
            {
                order.AssignedEmployeeId = request.AssignedEmployeeId;
            }

            if (request.DepartmentId != null)
            {
                order.DepartmentId = request.DepartmentId;
            }

            if (request.CustomerPoNumber != null)
            {
                order.CustomerPoNumber = request.CustomerPoNumber;
            }

            if (request.CustomerPoFileId.HasValue)
            {
                order.CustomerPoFileId = request.CustomerPoFileId;
            }

            if (request.BillingAddressId.HasValue)
            {
                order.BillingAddressId = request.BillingAddressId;
            }

            if (request.ShippingAddressId.HasValue)
            {
                order.ShippingAddressId = request.ShippingAddressId;
            }

            if (request.ShippingAddressLine1 != null)
            {
                order.ShippingAddressLine1 = request.ShippingAddressLine1;
            }

            if (request.ShippingAddressLine2 != null)
            {
                order.ShippingAddressLine2 = request.ShippingAddressLine2;
            }

            if (request.ShippingCity != null)
            {
                order.ShippingCity = request.ShippingCity;
            }

            if (request.ShippingProvince != null)
            {
                order.ShippingProvince = request.ShippingProvince;
            }

            if (request.ShippingPostalCode != null)
            {
                order.ShippingPostalCode = request.ShippingPostalCode;
            }

            if (request.ShippingCountry != null)
            {
                order.ShippingCountry = request.ShippingCountry;
            }

            if (request.BillingCompanyName != null)
            {
                order.BillingCompanyName = request.BillingCompanyName;
            }

            if (request.BillingVatNumber != null)
            {
                order.BillingVatNumber = request.BillingVatNumber;
            }

            if (request.DeliveryContactName != null)
            {
                order.DeliveryContactName = request.DeliveryContactName;
            }

            if (request.DeliveryContactPhone != null)
            {
                order.DeliveryContactPhone = request.DeliveryContactPhone;
            }

            if (request.DeliveryContactEmail != null)
            {
                order.DeliveryContactEmail = request.DeliveryContactEmail;
            }
        }

        // Service-specific attributes mappings
        /// <summary>Maps Order3DPrintingAttributes to Order3DPrintingAttributesDto</summary>
        public static Order3DPrintingAttributesDto ToPrintingAttributesDto(this Order3DPrintingAttributes attributes)
        {
            return new Order3DPrintingAttributesDto
            {
                ThreadTapRequired = attributes.ThreadTapRequired,
                InsertRequired = attributes.InsertRequired,
                PartMarking = attributes.PartMarking,
                PartAssemblyTestRequired = attributes.PartAssemblyTestRequired
            };
        }

        /// <summary>Maps OrderCncMachiningAttributes to OrderCncMachiningAttributesDto</summary>
        public static OrderCncMachiningAttributesDto ToCncAttributesDto(this OrderCncMachiningAttributes attributes)
        {
            return new OrderCncMachiningAttributesDto
            {
                TapRequired = attributes.TapRequired,
                Tolerance = attributes.Tolerance,
                SurfaceRoughness = attributes.SurfaceRoughness,
                InspectionType = attributes.InspectionType
            };
        }

        /// <summary>Maps OrderSheetMetalAttributes to OrderSheetMetalAttributesDto</summary>
        public static OrderSheetMetalAttributesDto ToSheetMetalAttributesDto(this OrderSheetMetalAttributes attributes)
        {
            return new OrderSheetMetalAttributesDto
            {
                Thickness = attributes.Thickness,
                WeldingRequired = attributes.WeldingRequired,
                WeldingDetails = attributes.WeldingDetails,
                Tolerance = attributes.Tolerance,
                InspectionType = attributes.InspectionType
            };
        }

        /// <summary>Maps Order3DScanningAttributes to Order3DScanningAttributesDto</summary>
        public static Order3DScanningAttributesDto ToScanningAttributesDto(this Order3DScanningAttributes attributes)
        {
            return new Order3DScanningAttributesDto
            {
                RequiredAccuracy = attributes.RequiredAccuracy,
                ScanLocation = attributes.ScanLocation,
                OutputFileFormats = attributes.OutputFileFormats,
                DeviationReportRequested = attributes.DeviationReportRequested
            };
        }

        /// <summary>Maps Order3DDesignAttributes to Order3DDesignAttributesDto</summary>
        public static Order3DDesignAttributesDto ToDesignAttributesDto(this Order3DDesignAttributes attributes)
        {
            return new Order3DDesignAttributesDto
            {
                ComplexityLevel = attributes.ComplexityLevel,
                Deliverables = attributes.Deliverables,
                DesignSoftware = attributes.DesignSoftware,
                RevisionRounds = attributes.RevisionRounds
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
                DeletedAt = file.DeletedAt,
                IsPrimary = file.IsPrimary
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
}

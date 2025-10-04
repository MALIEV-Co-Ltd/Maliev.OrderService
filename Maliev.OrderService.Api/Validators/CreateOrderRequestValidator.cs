using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;

namespace Maliev.OrderService.Api.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required")
            .MaximumLength(50).WithMessage("Customer ID must not exceed 50 characters");

        RuleFor(x => x.CustomerType)
            .NotEmpty().WithMessage("Customer Type is required")
            .Must(x => x == "Customer" || x == "Employee")
            .WithMessage("Customer Type must be 'Customer' or 'Employee'");

        RuleFor(x => x.ServiceCategoryId)
            .GreaterThan(0).WithMessage("Service Category ID must be greater than 0");

        RuleFor(x => x.ProcessTypeId)
            .GreaterThan(0).WithMessage("Process Type ID must be greater than 0")
            .When(x => x.ProcessTypeId.HasValue);

        RuleFor(x => x.Requirements)
            .MaximumLength(5000).WithMessage("Requirements must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.OrderedQuantity)
            .GreaterThan(0).WithMessage("Ordered Quantity must be greater than 0")
            .When(x => x.OrderedQuantity.HasValue);

        RuleFor(x => x.MaterialId)
            .GreaterThan(0).WithMessage("Material ID must be greater than 0")
            .When(x => x.MaterialId.HasValue);

        RuleFor(x => x.ColorId)
            .GreaterThan(0).WithMessage("Color ID must be greater than 0")
            .When(x => x.ColorId.HasValue);

        RuleFor(x => x.SurfaceFinishingId)
            .GreaterThan(0).WithMessage("Surface Finishing ID must be greater than 0")
            .When(x => x.SurfaceFinishingId.HasValue);

        RuleFor(x => x.LeadTimeDays)
            .GreaterThan(0).WithMessage("Lead Time Days must be greater than 0")
            .When(x => x.LeadTimeDays.HasValue);

        RuleFor(x => x.PromisedDeliveryDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Promised Delivery Date must be in the future")
            .When(x => x.PromisedDeliveryDate.HasValue);

        RuleFor(x => x.AssignedEmployeeId)
            .MaximumLength(50).WithMessage("Assigned Employee ID must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.AssignedEmployeeId));

        RuleFor(x => x.DepartmentId)
            .MaximumLength(50).WithMessage("Department ID must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.DepartmentId));
    }
}

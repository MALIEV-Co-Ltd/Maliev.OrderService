using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;

namespace Maliev.OrderService.Api.Validators;

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version (RowVersion) is required for optimistic concurrency");

        RuleFor(x => x.Requirements)
            .MaximumLength(5000).WithMessage("Requirements must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.OrderedQuantity)
            .GreaterThan(0).WithMessage("Ordered Quantity must be greater than 0")
            .When(x => x.OrderedQuantity.HasValue);

        RuleFor(x => x.ManufacturedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Manufactured Quantity must be greater than or equal to 0")
            .When(x => x.ManufacturedQuantity.HasValue);

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

        RuleFor(x => x.QuotedAmount)
            .GreaterThan(0).WithMessage("Quoted Amount must be greater than 0")
            .When(x => x.QuotedAmount.HasValue);

        RuleFor(x => x.QuoteCurrency)
            .Length(3).WithMessage("Quote Currency must be a 3-letter ISO currency code (e.g., THB, USD)")
            .When(x => !string.IsNullOrEmpty(x.QuoteCurrency));

        RuleFor(x => x.AssignedEmployeeId)
            .MaximumLength(50).WithMessage("Assigned Employee ID must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.AssignedEmployeeId));

        RuleFor(x => x.DepartmentId)
            .MaximumLength(50).WithMessage("Department ID must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.DepartmentId));
    }
}

using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;

namespace Maliev.OrderService.Api.Validators;

public class CreateOrderStatusRequestValidator : AbstractValidator<CreateOrderStatusRequest>
{
    private static readonly string[] ValidStatuses = new[]
    {
        "New", "Reviewing", "Rejected", "Reviewed", "Quoted", "Declined", "Accepted", "Expired",
        "Paid", "POIssued", "InProgress", "OnHold", "Finished", "Shipped", "Reopen", "Cancelled"
    };

    public CreateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => ValidStatuses.Contains(status))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");

        RuleFor(x => x.InternalNotes)
            .MaximumLength(2000).WithMessage("Internal Notes must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.InternalNotes));

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(2000).WithMessage("Customer Notes must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerNotes));
    }
}

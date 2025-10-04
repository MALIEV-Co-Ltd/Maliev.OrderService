using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;

namespace Maliev.OrderService.Api.Validators;

public class CreateOrderNoteRequestValidator : AbstractValidator<CreateOrderNoteRequest>
{
    private static readonly string[] ValidNoteTypes = new[] { "customer", "internal" };

    public CreateOrderNoteRequestValidator()
    {
        RuleFor(x => x.NoteType)
            .NotEmpty().WithMessage("Note Type is required")
            .Must(type => ValidNoteTypes.Contains(type))
            .WithMessage($"Note Type must be one of: {string.Join(", ", ValidNoteTypes)}");

        RuleFor(x => x.NoteText)
            .NotEmpty().WithMessage("Note Text is required")
            .MaximumLength(2000).WithMessage("Note Text must not exceed 2000 characters");
    }
}

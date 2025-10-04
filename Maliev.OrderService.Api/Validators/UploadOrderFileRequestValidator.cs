using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;

namespace Maliev.OrderService.Api.Validators;

public class UploadOrderFileRequestValidator : AbstractValidator<UploadOrderFileRequest>
{
    private static readonly string[] ValidFileRoles = new[] { "Input", "Output", "Supporting" };
    private static readonly string[] ValidFileCategories = new[] { "CAD", "Drawing", "Image", "Document", "Archive", "Other" };
    private static readonly string[] ValidDesignUnits = new[] { "mm", "inch", "cm", "m" };

    public UploadOrderFileRequestValidator()
    {
        RuleFor(x => x.FileRole)
            .NotEmpty().WithMessage("File Role is required")
            .Must(role => ValidFileRoles.Contains(role))
            .WithMessage($"File Role must be one of: {string.Join(", ", ValidFileRoles)}");

        RuleFor(x => x.FileCategory)
            .NotEmpty().WithMessage("File Category is required")
            .Must(category => ValidFileCategories.Contains(category))
            .WithMessage($"File Category must be one of: {string.Join(", ", ValidFileCategories)}");

        RuleFor(x => x.DesignUnits)
            .Must(units => ValidDesignUnits.Contains(units!))
            .WithMessage($"Design Units must be one of: {string.Join(", ", ValidDesignUnits)}")
            .When(x => !string.IsNullOrEmpty(x.DesignUnits));
    }
}

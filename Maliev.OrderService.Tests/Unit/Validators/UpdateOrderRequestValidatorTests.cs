using FluentAssertions;
using FluentValidation.TestHelper;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Validators;

namespace Maliev.OrderService.Tests.Unit.Validators;

public class UpdateOrderRequestValidatorTests
{
    private readonly UpdateOrderRequestValidator _validator;

    public UpdateOrderRequestValidatorTests()
    {
        _validator = new UpdateOrderRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            AssignedEmployeeId = "EMP-001"
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyVersion_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "",
            AssignedEmployeeId = "EMP-001"
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("Version (RowVersion) is required for optimistic concurrency");
    }

    [Fact]
    public async Task Validate_WithRequirementsTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            Requirements = new string('A', 5001)
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Requirements)
            .WithErrorMessage("Requirements must not exceed 5000 characters");
    }

    [Fact]
    public async Task Validate_WithNegativeOrderedQuantity_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            OrderedQuantity = -5
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedQuantity)
            .WithErrorMessage("Ordered Quantity must be greater than 0");
    }

    [Fact]
    public async Task Validate_WithNegativeManufacturedQuantity_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            ManufacturedQuantity = -1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ManufacturedQuantity)
            .WithErrorMessage("Manufactured Quantity must be greater than or equal to 0");
    }

    [Fact]
    public async Task Validate_WithZeroManufacturedQuantity_ShouldNotHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            ManufacturedQuantity = 0
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ManufacturedQuantity);
    }

    [Fact]
    public async Task Validate_WithNegativeQuotedAmount_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            QuotedAmount = -100.50m
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.QuotedAmount)
            .WithErrorMessage("Quoted Amount must be greater than 0");
    }

    [Fact]
    public async Task Validate_WithInvalidQuoteCurrencyLength_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            QuoteCurrency = "US"
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.QuoteCurrency)
            .WithErrorMessage("Quote Currency must be a 3-letter ISO currency code (e.g., THB, USD)");
    }

    [Fact]
    public async Task Validate_WithValidQuoteCurrency_ShouldNotHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            QuoteCurrency = "THB"
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.QuoteCurrency);
    }

    [Fact]
    public async Task Validate_WithAssignedEmployeeIdTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            AssignedEmployeeId = new string('A', 51)
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssignedEmployeeId)
            .WithErrorMessage("Assigned Employee ID must not exceed 50 characters");
    }

    [Fact]
    public async Task Validate_WithDepartmentIdTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            DepartmentId = new string('A', 51)
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DepartmentId)
            .WithErrorMessage("Department ID must not exceed 50 characters");
    }

    [Fact]
    public async Task Validate_WithNegativeMaterialId_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            MaterialId = -1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaterialId)
            .WithErrorMessage("Material ID must be greater than 0");
    }

    [Fact]
    public async Task Validate_WithNegativeLeadTimeDays_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Version = "AAAAAAAAB9E=",
            LeadTimeDays = -5
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LeadTimeDays)
            .WithErrorMessage("Lead Time Days must be greater than 0");
    }
}

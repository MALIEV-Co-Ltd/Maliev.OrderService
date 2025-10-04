using FluentAssertions;
using FluentValidation.TestHelper;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Validators;

namespace Maliev.OrderService.Tests.Unit.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator;

    public CreateOrderRequestValidatorTests()
    {
        _validator = new CreateOrderRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyCustomerId_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required");
    }

    [Fact]
    public async Task Validate_WithCustomerIdTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = new string('A', 51),
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID must not exceed 50 characters");
    }

    [Fact]
    public async Task Validate_WithEmptyCustomerType_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerType);
    }

    [Fact]
    public async Task Validate_WithInvalidCustomerType_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "InvalidType",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerType)
            .WithErrorMessage("Customer Type must be 'Customer' or 'Employee'");
    }

    [Fact]
    public async Task Validate_WithCustomerTypeEmployee_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "EMP-001",
            CustomerType = "Employee",
            ServiceCategoryId = 1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerType);
    }

    [Fact]
    public async Task Validate_WithZeroServiceCategoryId_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 0
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServiceCategoryId)
            .WithErrorMessage("Service Category ID must be greater than 0");
    }

    [Fact]
    public async Task Validate_WithNegativeServiceCategoryId_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = -1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServiceCategoryId);
    }

    [Fact]
    public async Task Validate_WithRequirementsTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
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
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            OrderedQuantity = -5
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedQuantity)
            .WithErrorMessage("Ordered Quantity must be greater than 0");
    }

    [Fact]
    public async Task Validate_WithPastPromisedDeliveryDate_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            PromisedDeliveryDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PromisedDeliveryDate)
            .WithErrorMessage("Promised Delivery Date must be in the future");
    }

    [Fact]
    public async Task Validate_WithFuturePromisedDeliveryDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            PromisedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PromisedDeliveryDate);
    }

    [Fact]
    public async Task Validate_WithNegativeMaterialId_ShouldHaveError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            MaterialId = -1
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaterialId)
            .WithErrorMessage("Material ID must be greater than 0");
    }
}

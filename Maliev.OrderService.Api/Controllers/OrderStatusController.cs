using Asp.Versioning;
using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.OrderService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/orders/{orderId}/statuses")]
[Authorize(Policy = "EmployeeOrHigher")]
[EnableRateLimiting("general")]
public class OrderStatusController : ControllerBase
{
    private readonly IOrderStatusService _statusService;
    private readonly IValidator<CreateOrderStatusRequest> _validator;
    private readonly ILogger<OrderStatusController> _logger;

    public OrderStatusController(
        IOrderStatusService statusService,
        IValidator<CreateOrderStatusRequest> validator,
        ILogger<OrderStatusController> logger)
    {
        _statusService = statusService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderStatus(
        string orderId,
        [FromBody] CreateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        try
        {
            var updatedBy = "system"; // TODO: Get from user context
            var status = await _statusService.CreateOrderStatusAsync(orderId, request, updatedBy, cancellationToken);
            return CreatedAtRoute(new { orderId }, status);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Lightweight business metrics for dashboards.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("order/v{version:apiVersion}/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IOrderManagementService _orderManagementService;
        private readonly ILogger<MetricsController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="MetricsController"/>.
        /// </summary>
        public MetricsController(IOrderManagementService orderManagementService, ILogger<MetricsController> logger)
        {
            _orderManagementService = orderManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Get the count of active orders.
        /// </summary>
        [HttpGet("active-count")]
        [RequirePermission(OrderPermissions.ReportsAnalytics)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveCount(CancellationToken cancellationToken)
        {
            // Use GetOrdersAsync with status filter to count active orders
            // For OrderService, "Active" status might depend on the implementation.
            // We'll filter by "Open" or similar if possible, or just get the count from total.
            var response = await _orderManagementService.GetOrdersAsync(
                page: 1,
                pageSize: 1, // We only need the totalCount
                user: User,
                status: "Open", // Assuming "Open" is an active status
                cancellationToken: cancellationToken);

            return Ok(new { count = response.TotalCount });
        }
    }
}

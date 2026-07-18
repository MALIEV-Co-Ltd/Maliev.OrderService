using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Lightweight business metrics for dashboards (active-count, on-hold-count).
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
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
            var response = await _orderManagementService.GetOrdersAsync(
                page: 1,
                pageSize: 1,
                user: User,
                status: "Open",
                cancellationToken: cancellationToken);

            return Ok(new { count = response.TotalCount });
        }

        /// <summary>
        /// Get the count of on-hold orders.
        /// </summary>
        [HttpGet("on-hold-count")]
        [RequirePermission(OrderPermissions.ReportsAnalytics)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOnHoldCount(CancellationToken cancellationToken)
        {
            var response = await _orderManagementService.GetOrdersAsync(
                page: 1,
                pageSize: 1,
                user: User,
                status: "OnHold",
                cancellationToken: cancellationToken);

            return Ok(new { count = response.TotalCount });
        }
    }
}

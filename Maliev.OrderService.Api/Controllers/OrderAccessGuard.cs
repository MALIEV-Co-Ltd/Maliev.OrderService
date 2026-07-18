using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Controllers
{
    internal static class OrderAccessGuard
    {
        public static async Task<IActionResult?> EnsureCanAccessOrderAsync(
            ControllerBase controller,
            OrderDbContext context,
            IOrderAuthorizationService orderAuthService,
            string orderId,
            CancellationToken cancellationToken,
            bool notFoundAsBadRequest = false)
        {
            Order? order = await context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            return order == null
                ? BuildNotFoundResult(controller, orderId, notFoundAsBadRequest)
                : orderAuthService.CanViewOrder(controller.User, order)
                    ? null
                    : controller.Forbid();
        }

        private static IActionResult BuildNotFoundResult(
            ControllerBase controller,
            string orderId,
            bool notFoundAsBadRequest)
        {
            return notFoundAsBadRequest
                ? controller.BadRequest(new { message = $"Order {orderId} not found" })
                : controller.NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
        }
    }
}

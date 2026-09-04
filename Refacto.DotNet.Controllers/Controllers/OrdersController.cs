using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Processes an order: for each item, decrements stock or triggers the
        /// relevant customer notification depending on the product's type and
        /// availability.
        /// </summary>
        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(typeof(ProcessOrderResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId, CancellationToken cancellationToken)
        {
            Order? order = await _orderService.ProcessOrderAsync(orderId, cancellationToken);

            if (order is null)
            {
                return NotFound();
            }

            return Ok(new ProcessOrderResponse(order.Id));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Application.Orders;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderProcessor _orderProcessor;

        public OrdersController(IOrderProcessor orderProcessor)
        {
            _orderProcessor = orderProcessor;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(typeof(ProcessOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(
            long orderId,
            CancellationToken cancellationToken)
        {
            Order? order = await _orderProcessor.ProcessAsync(orderId, cancellationToken);

            return order is null
                ? NotFound()
                : new ProcessOrderResponse(order.Id);
        }
    }
}

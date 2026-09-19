using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Infrastructure.Persistence;

namespace Refacto.DotNet.Controllers.Application.Orders
{
    public sealed class OrderProcessor : IOrderProcessor
    {
        private readonly IOrderRepository _orders;
        private readonly IProductHandlerResolver _handlers;

        public OrderProcessor(IOrderRepository orders, IProductHandlerResolver handlers)
        {
            _orders = orders;
            _handlers = handlers;
        }

        public async Task<Order?> ProcessAsync(long orderId, CancellationToken cancellationToken = default)
        {
            Order? order = await _orders.FindWithItemsAsync(orderId, cancellationToken);
            if (order is null)
            {
                return null;
            }

            foreach (Product product in order.Items ?? Array.Empty<Product>())
            {
                _handlers.Resolve(product.Type).Handle(product);
            }

            await _orders.SaveChangesAsync(cancellationToken);

            return order;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Stock;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Application-layer service orchestrating order processing: it loads the order,
    /// delegates the per-product business rules to the matching <see cref="IProductStockHandler"/>,
    /// and persists the result in a single transaction.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IProductStockHandlerResolver _stockHandlerResolver;

        public OrderService(AppDbContext context, IProductStockHandlerResolver stockHandlerResolver)
        {
            _context = context;
            _stockHandlerResolver = stockHandlerResolver;
        }

        public async Task<Order?> ProcessOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order?.Items is null)
            {
                return order;
            }

            foreach (Product product in order.Items)
            {
                IProductStockHandler handler = _stockHandlerResolver.Resolve(product.Type);
                handler.Process(product);
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            return order;
        }
    }
}

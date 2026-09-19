using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Infrastructure.Persistence
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Order?> FindWithItemsAsync(long orderId, CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .Include(order => order.Items)
                .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}

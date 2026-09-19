using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Infrastructure.Persistence
{
    public interface IOrderRepository
    {
        Task<Order?> FindWithItemsAsync(long orderId, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

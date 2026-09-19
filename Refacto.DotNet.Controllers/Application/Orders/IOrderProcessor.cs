using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Application.Orders
{
    public interface IOrderProcessor
    {
        Task<Order?> ProcessAsync(long orderId, CancellationToken cancellationToken = default);
    }
}

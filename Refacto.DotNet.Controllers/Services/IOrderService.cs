using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IOrderService
    {
        /// <summary>
        /// Processes every item of the given order (updating stock and triggering
        /// notifications as needed) and persists the result.
        /// </summary>
        /// <returns>The processed order, or null if no order exists with that id.</returns>
        Task<Order?> ProcessOrderAsync(long orderId, CancellationToken cancellationToken = default);
    }
}

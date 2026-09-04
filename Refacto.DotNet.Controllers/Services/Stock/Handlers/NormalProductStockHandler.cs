using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock.Handlers
{
    /// <summary>
    /// NORMAL products have no particular constraint: while in stock, ordering
    /// one simply decrements the available quantity. Once out of stock, the
    /// customer is informed of the restocking lead time (if any).
    /// </summary>
    public class NormalProductStockHandler : IProductStockHandler
    {
        private readonly INotificationService _notificationService;

        public NormalProductStockHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public ProductType ProductType => ProductType.Normal;

        public void Process(Product product)
        {
            if (product.Available > 0)
            {
                product.Available -= 1;
                return;
            }

            if (product.LeadTime > 0)
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.Name ?? string.Empty);
            }
        }
    }
}

using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock.Handlers
{
    /// <summary>
    /// EXPIRABLE products can be sold normally until their ExpiryDate. Once expired
    /// (or if there is no stock left), the product becomes unavailable (stock forced
    /// to 0) and the customer is notified of the expiration.
    /// </summary>
    public class ExpirableProductStockHandler : IProductStockHandler
    {
        private readonly INotificationService _notificationService;

        public ExpirableProductStockHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public ProductType ProductType => ProductType.Expirable;

        public void Process(Product product)
        {
            bool hasNotExpired = product.ExpiryDate > DateTime.Now.Date;

            if (product.Available > 0 && hasNotExpired)
            {
                product.Available -= 1;
                return;
            }

            product.Available = 0;
            if (product.ExpiryDate.HasValue)
            {
                _notificationService.SendExpirationNotification(product.Name ?? string.Empty, product.ExpiryDate.Value);
            }
        }
    }
}

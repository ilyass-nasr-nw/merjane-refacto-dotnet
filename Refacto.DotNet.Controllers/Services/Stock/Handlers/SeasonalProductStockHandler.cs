using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock.Handlers
{
    /// <summary>
    /// SEASONAL products are only sellable during their [SeasonStartDate, SeasonEndDate]
    /// window. When out of stock:
    ///   - if restocking (LeadTime) would finish after the season ends, the product
    ///     becomes unavailable for the rest of the season (stock forced to 0) and the
    ///     customer is notified it is unavailable.
    ///   - if the season has not started yet, the customer is notified it is unavailable.
    ///   - otherwise there is still time to restock within the season: the customer is
    ///     only notified of the delay.
    /// </summary>
    public class SeasonalProductStockHandler : IProductStockHandler
    {
        private readonly INotificationService _notificationService;

        public SeasonalProductStockHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public ProductType ProductType => ProductType.Seasonal;

        public void Process(Product product)
        {
            if (IsInSeason(product) && product.Available > 0)
            {
                product.Available -= 1;
                return;
            }

            if (RestockWouldMissTheSeason(product))
            {
                product.Available = 0;
                _notificationService.SendOutOfStockNotification(product.Name ?? string.Empty);
            }
            else if (product.SeasonStartDate > DateTime.Now)
            {
                _notificationService.SendOutOfStockNotification(product.Name ?? string.Empty);
            }
            else
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.Name ?? string.Empty);
            }
        }

        private static bool IsInSeason(Product product)
        {
            DateTime today = DateTime.Now.Date;
            return today > product.SeasonStartDate && today < product.SeasonEndDate;
        }

        private static bool RestockWouldMissTheSeason(Product product) =>
            DateTime.Now.AddDays(product.LeadTime) > product.SeasonEndDate;
    }
}

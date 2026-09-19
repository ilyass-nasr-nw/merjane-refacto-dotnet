using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Infrastructure.Time;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public sealed class SeasonalProductHandler : IProductHandler
    {
        private readonly INotificationService _notifications;
        private readonly IDateTimeProvider _clock;

        public SeasonalProductHandler(INotificationService notifications, IDateTimeProvider clock)
        {
            _notifications = notifications;
            _clock = clock;
        }

        public ProductType HandledType => ProductType.Seasonal;

        public void Handle(Product product)
        {
            if (IsInSeason(product) && product.Available > 0)
            {
                product.Available -= 1;
                return;
            }

            if (RestockingOutlastsTheSeason(product))
            {
                _notifications.SendOutOfStockNotification(product.Name);
                product.Available = 0;
                return;
            }

            if (SeasonHasNotStarted(product))
            {
                _notifications.SendOutOfStockNotification(product.Name);
                return;
            }

            if (product.LeadTime > 0)
            {
                _notifications.SendDelayNotification(product.LeadTime, product.Name);
            }
        }

        private bool IsInSeason(Product product) =>
            _clock.Today >= product.SeasonStartDate && _clock.Today <= product.SeasonEndDate;

        private bool RestockingOutlastsTheSeason(Product product) =>
            _clock.Today.AddDays(product.LeadTime) > product.SeasonEndDate;

        private bool SeasonHasNotStarted(Product product) =>
            product.SeasonStartDate > _clock.Today;
    }
}

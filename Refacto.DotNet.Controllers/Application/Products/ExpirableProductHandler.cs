using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Infrastructure.Time;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public sealed class ExpirableProductHandler : IProductHandler
    {
        private readonly INotificationService _notifications;
        private readonly IDateTimeProvider _clock;

        public ExpirableProductHandler(INotificationService notifications, IDateTimeProvider clock)
        {
            _notifications = notifications;
            _clock = clock;
        }

        public ProductType HandledType => ProductType.Expirable;

        public void Handle(Product product)
        {
            if (HasExpired(product))
            {
                _notifications.SendExpirationNotification(product.Name, product.ExpiryDate!.Value);
                product.Available = 0;
                return;
            }

            if (product.Available > 0)
            {
                product.Available -= 1;
                return;
            }

            if (product.LeadTime > 0)
            {
                _notifications.SendDelayNotification(product.LeadTime, product.Name);
            }
        }

        private bool HasExpired(Product product) =>
            product.ExpiryDate is not null && product.ExpiryDate < _clock.Today;
    }
}

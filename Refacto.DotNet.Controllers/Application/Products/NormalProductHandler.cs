using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public sealed class NormalProductHandler : IProductHandler
    {
        private readonly INotificationService _notifications;

        public NormalProductHandler(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public ProductType HandledType => ProductType.Normal;

        public void Handle(Product product)
        {
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
    }
}

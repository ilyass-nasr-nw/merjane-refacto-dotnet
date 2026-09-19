using Moq;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Application.Products
{
    public class NormalProductHandlerTests
    {
        private readonly Mock<INotificationService> _notifications = new();
        private readonly NormalProductHandler _handler;

        public NormalProductHandlerTests()
        {
            _handler = new NormalProductHandler(_notifications.Object);
        }

        [Fact]
        public void It_handles_normal_products()
        {
            Assert.Equal(ProductType.Normal, _handler.HandledType);
        }

        [Fact]
        public void An_available_product_is_decremented_and_nobody_is_notified()
        {
            Product product = ANormalProduct(available: 30);

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void An_out_of_stock_product_announces_its_restocking_delay()
        {
            Product product = ANormalProduct(available: 0, leadTime: 10);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void An_out_of_stock_product_without_a_lead_time_stays_silent()
        {
            Product product = ANormalProduct(available: 0, leadTime: 0);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        private static Product ANormalProduct(int available, int leadTime = 15) => new()
        {
            Type = ProductType.Normal,
            Name = "USB Dongle",
            Available = available,
            LeadTime = leadTime
        };
    }
}

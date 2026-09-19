using Moq;
using Refacto.Dotnet.Controllers.Tests.Fakes;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Application.Products
{
    public class ExpirableProductHandlerTests
    {
        private static readonly DateTime Today = FakeDateTimeProvider.DefaultNow.Date;

        private readonly Mock<INotificationService> _notifications = new();
        private readonly FakeDateTimeProvider _clock = new();
        private readonly ExpirableProductHandler _handler;

        public ExpirableProductHandlerTests()
        {
            _handler = new ExpirableProductHandler(_notifications.Object, _clock);
        }

        [Fact]
        public void It_handles_expirable_products()
        {
            Assert.Equal(ProductType.Expirable, _handler.HandledType);
        }

        [Fact]
        public void A_fresh_product_in_stock_is_decremented()
        {
            Product product = AnExpirableProduct(available: 30, expiryDate: Today.AddDays(26));

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void An_expired_product_is_reported_expired_and_pulled_from_the_catalogue()
        {
            DateTime expiry = Today.AddDays(-2);
            Product product = AnExpirableProduct(available: 6, expiryDate: expiry);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendExpirationNotification("Milk", expiry), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void On_its_expiry_date_the_product_is_still_sellable()
        {
            Product product = AnExpirableProduct(available: 6, expiryDate: Today);

            _handler.Handle(product);

            Assert.Equal(5, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void A_fresh_product_that_is_merely_out_of_stock_announces_a_delay()
        {
            Product product = AnExpirableProduct(available: 0, expiryDate: Today.AddDays(30), leadTime: 7);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendDelayNotification(7, "Milk"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void A_fresh_product_out_of_stock_without_a_lead_time_stays_silent()
        {
            Product product = AnExpirableProduct(available: 0, expiryDate: Today.AddDays(30), leadTime: 0);

            _handler.Handle(product);

            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void A_product_without_an_expiry_date_never_expires()
        {
            Product product = AnExpirableProduct(available: 4, expiryDate: null);

            _handler.Handle(product);

            Assert.Equal(3, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        private static Product AnExpirableProduct(int available, DateTime? expiryDate, int leadTime = 15) => new()
        {
            Type = ProductType.Expirable,
            Name = "Milk",
            Available = available,
            LeadTime = leadTime,
            ExpiryDate = expiryDate
        };
    }
}

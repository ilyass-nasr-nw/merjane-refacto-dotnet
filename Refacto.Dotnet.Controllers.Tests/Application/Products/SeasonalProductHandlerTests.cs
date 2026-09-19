using Moq;
using Refacto.Dotnet.Controllers.Tests.Fakes;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Application.Products
{
    public class SeasonalProductHandlerTests
    {
        private static readonly DateTime Today = FakeDateTimeProvider.DefaultNow.Date;

        private readonly Mock<INotificationService> _notifications = new();
        private readonly FakeDateTimeProvider _clock = new();
        private readonly SeasonalProductHandler _handler;

        public SeasonalProductHandlerTests()
        {
            _handler = new SeasonalProductHandler(_notifications.Object, _clock);
        }

        [Fact]
        public void It_handles_seasonal_products()
        {
            Assert.Equal(ProductType.Seasonal, _handler.HandledType);
        }

        [Fact]
        public void In_season_and_in_stock_the_product_is_decremented()
        {
            Product product = ASeasonalProduct(available: 30, seasonStart: Today.AddDays(-10), seasonEnd: Today.AddDays(40));

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void On_the_first_day_of_the_season_the_product_is_already_sellable()
        {
            Product product = ASeasonalProduct(available: 30, seasonStart: Today, seasonEnd: Today.AddDays(40));

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void On_the_last_day_of_the_season_the_product_is_still_sellable()
        {
            Product product = ASeasonalProduct(available: 30, leadTime: 1, seasonStart: Today.AddDays(-40), seasonEnd: Today);

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void A_season_that_has_not_started_is_reported_unavailable_but_keeps_its_stock()
        {
            Product product = ASeasonalProduct(available: 30, leadTime: 15, seasonStart: Today.AddDays(180), seasonEnd: Today.AddDays(240));

            _handler.Handle(product);

            Assert.Equal(30, product.Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void In_season_but_out_of_stock_the_restocking_delay_is_announced()
        {
            Product product = ASeasonalProduct(available: 0, leadTime: 5, seasonStart: Today.AddDays(-10), seasonEnd: Today.AddDays(40));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendDelayNotification(5, "Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void In_season_but_out_of_stock_without_a_lead_time_stays_silent()
        {
            Product product = ASeasonalProduct(available: 0, leadTime: 0, seasonStart: Today.AddDays(-10), seasonEnd: Today.AddDays(40));

            _handler.Handle(product);

            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void When_restocking_lands_after_the_season_the_product_is_marked_unavailable()
        {
            Product product = ASeasonalProduct(available: 0, leadTime: 30, seasonStart: Today.AddDays(-10), seasonEnd: Today.AddDays(5));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void Restocking_that_lands_exactly_on_the_last_day_of_the_season_still_announces_a_delay()
        {
            Product product = ASeasonalProduct(available: 0, leadTime: 10, seasonStart: Today.AddDays(-5), seasonEnd: Today.AddDays(10));

            _handler.Handle(product);

            _notifications.Verify(n => n.SendDelayNotification(10, "Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void Restocking_past_the_season_end_wins_over_a_season_that_has_not_started()
        {
            Product product = ASeasonalProduct(available: 30, leadTime: 120, seasonStart: Today.AddDays(30), seasonEnd: Today.AddDays(90));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public void A_product_without_season_dates_falls_back_to_announcing_a_delay()
        {
            Product product = ASeasonalProduct(available: 0, leadTime: 7, seasonStart: null, seasonEnd: null);

            _handler.Handle(product);

            _notifications.Verify(n => n.SendDelayNotification(7, "Watermelon"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        private static Product ASeasonalProduct(
            int available,
            DateTime? seasonStart,
            DateTime? seasonEnd,
            int leadTime = 15) => new()
            {
                Type = ProductType.Seasonal,
                Name = "Watermelon",
                Available = available,
                LeadTime = leadTime,
                SeasonStartDate = seasonStart,
                SeasonEndDate = seasonEnd
            };
    }
}

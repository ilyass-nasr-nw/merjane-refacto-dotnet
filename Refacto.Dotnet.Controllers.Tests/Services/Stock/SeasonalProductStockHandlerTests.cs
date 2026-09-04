using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Stock.Handlers;

namespace Refacto.DotNet.Controllers.Tests.Services.Stock
{
    public class SeasonalProductStockHandlerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly SeasonalProductStockHandler _handler;

        public SeasonalProductStockHandlerTests()
        {
            _handler = new SeasonalProductStockHandler(_notificationServiceMock.Object);
        }

        [Fact]
        public void Process_WhenInSeasonAndInStock_DecrementsAvailable()
        {
            Product product = new()
            {
                Type = ProductType.Seasonal,
                Available = 30,
                LeadTime = 15,
                Name = "Watermelon",
                SeasonStartDate = DateTime.Now.AddDays(-2),
                SeasonEndDate = DateTime.Now.AddDays(58)
            };

            _handler.Process(product);

            Assert.Equal(29, product.Available);
            _notificationServiceMock.Verify(
                s => s.SendOutOfStockNotification(It.IsAny<string>()), Times.Never());
            _notificationServiceMock.Verify(
                s => s.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void Process_WhenSeasonHasNotStartedYet_NotifiesOutOfStockAndKeepsAvailableUntouched()
        {
            Product product = new()
            {
                Type = ProductType.Seasonal,
                Available = 30,
                LeadTime = 15,
                Name = "Grapes",
                SeasonStartDate = DateTime.Now.AddDays(180),
                SeasonEndDate = DateTime.Now.AddDays(240)
            };

            _handler.Process(product);

            _notificationServiceMock.Verify(s => s.SendOutOfStockNotification("Grapes"), Times.Once());
        }

        [Fact]
        public void Process_WhenOutOfStockAndRestockWouldMissTheSeason_MarksUnavailableAndNotifies()
        {
            Product product = new()
            {
                Type = ProductType.Seasonal,
                Available = 0,
                LeadTime = 15,
                Name = "Strawberries",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(5)
            };

            _handler.Process(product);

            Assert.Equal(0, product.Available);
            _notificationServiceMock.Verify(s => s.SendOutOfStockNotification("Strawberries"), Times.Once());
        }

        [Fact]
        public void Process_WhenOutOfStockButRestockFitsWithinSeason_SendsDelayNotification()
        {
            Product product = new()
            {
                Type = ProductType.Seasonal,
                Available = 0,
                LeadTime = 5,
                Name = "Cherries",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(60)
            };

            _handler.Process(product);

            _notificationServiceMock.Verify(s => s.SendDelayNotification(5, "Cherries"), Times.Once());
        }
    }
}

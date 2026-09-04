using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Stock.Handlers;

namespace Refacto.DotNet.Controllers.Tests.Services.Stock
{
    public class ExpirableProductStockHandlerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly ExpirableProductStockHandler _handler;

        public ExpirableProductStockHandlerTests()
        {
            _handler = new ExpirableProductStockHandler(_notificationServiceMock.Object);
        }

        [Fact]
        public void Process_WhenInStockAndNotExpired_DecrementsAvailable()
        {
            Product product = new()
            {
                Type = ProductType.Expirable,
                Available = 30,
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(26)
            };

            _handler.Process(product);

            Assert.Equal(29, product.Available);
            _notificationServiceMock.Verify(
                s => s.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never());
        }

        [Fact]
        public void Process_WhenExpired_MarksUnavailableAndNotifies()
        {
            DateTime expiryDate = DateTime.Now.AddDays(-2);
            Product product = new()
            {
                Type = ProductType.Expirable,
                Available = 6,
                Name = "Milk",
                ExpiryDate = expiryDate
            };

            _handler.Process(product);

            Assert.Equal(0, product.Available);
            _notificationServiceMock.Verify(s => s.SendExpirationNotification("Milk", expiryDate), Times.Once());
        }

        [Fact]
        public void Process_WhenOutOfStockButNotExpired_MarksUnavailableAndNotifies()
        {
            DateTime expiryDate = DateTime.Now.AddDays(10);
            Product product = new()
            {
                Type = ProductType.Expirable,
                Available = 0,
                Name = "Yogurt",
                ExpiryDate = expiryDate
            };

            _handler.Process(product);

            Assert.Equal(0, product.Available);
            _notificationServiceMock.Verify(s => s.SendExpirationNotification("Yogurt", expiryDate), Times.Once());
        }
    }
}

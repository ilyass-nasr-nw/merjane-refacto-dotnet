using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Stock.Handlers;

namespace Refacto.DotNet.Controllers.Tests.Services.Stock
{
    public class NormalProductStockHandlerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly NormalProductStockHandler _handler;

        public NormalProductStockHandlerTests()
        {
            _handler = new NormalProductStockHandler(_notificationServiceMock.Object);
        }

        [Fact]
        public void Process_WhenInStock_DecrementsAvailableAndDoesNotNotify()
        {
            Product product = new() { Type = ProductType.Normal, Available = 5, LeadTime = 15, Name = "USB Cable" };

            _handler.Process(product);

            Assert.Equal(4, product.Available);
            _notificationServiceMock.Verify(
                s => s.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void Process_WhenOutOfStockWithLeadTime_SendsDelayNotification()
        {
            Product product = new() { Type = ProductType.Normal, Available = 0, LeadTime = 15, Name = "RJ45 Cable" };

            _handler.Process(product);

            Assert.Equal(0, product.Available);
            _notificationServiceMock.Verify(s => s.SendDelayNotification(15, "RJ45 Cable"), Times.Once());
        }

        [Fact]
        public void Process_WhenOutOfStockWithNoLeadTime_DoesNotNotify()
        {
            Product product = new() { Type = ProductType.Normal, Available = 0, LeadTime = 0, Name = "RJ45 Cable" };

            _handler.Process(product);

            _notificationServiceMock.Verify(
                s => s.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }
    }
}

using Moq;
using Refacto.DotNet.Controllers.Application.Orders;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Infrastructure.Persistence;

namespace Refacto.Dotnet.Controllers.Tests.Application.Orders
{
    public class OrderProcessorTests
    {
        private readonly Mock<IOrderRepository> _orders = new();
        private readonly Mock<IProductHandlerResolver> _resolver = new();
        private readonly OrderProcessor _processor;

        public OrderProcessorTests()
        {
            _processor = new OrderProcessor(_orders.Object, _resolver.Object);
        }

        [Fact]
        public async Task Every_item_is_handed_to_the_handler_of_its_own_type()
        {
            Product normal = AProduct(ProductType.Normal);
            Product seasonal = AProduct(ProductType.Seasonal);
            Mock<IProductHandler> normalHandler = new();
            Mock<IProductHandler> seasonalHandler = new();
            _resolver.Setup(r => r.Resolve(ProductType.Normal)).Returns(normalHandler.Object);
            _resolver.Setup(r => r.Resolve(ProductType.Seasonal)).Returns(seasonalHandler.Object);
            GivenTheOrder(42, normal, seasonal);

            await _processor.ProcessAsync(42);

            normalHandler.Verify(h => h.Handle(normal), Times.Once);
            seasonalHandler.Verify(h => h.Handle(seasonal), Times.Once);
            normalHandler.VerifyNoOtherCalls();
            seasonalHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task The_whole_order_is_persisted_in_a_single_save()
        {
            Mock<IProductHandler> handler = new();
            _resolver.Setup(r => r.Resolve(It.IsAny<ProductType>())).Returns(handler.Object);
            GivenTheOrder(42, AProduct(ProductType.Normal), AProduct(ProductType.Normal), AProduct(ProductType.Normal));

            await _processor.ProcessAsync(42);

            _orders.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task The_processed_order_is_returned()
        {
            _resolver.Setup(r => r.Resolve(It.IsAny<ProductType>())).Returns(Mock.Of<IProductHandler>());
            GivenTheOrder(42, AProduct(ProductType.Normal));

            Order? result = await _processor.ProcessAsync(42);

            Assert.NotNull(result);
            Assert.Equal(42, result.Id);
        }

        [Fact]
        public async Task An_unknown_order_returns_null_and_saves_nothing()
        {
            _orders.Setup(o => o.FindWithItemsAsync(404, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Order?)null);

            Order? result = await _processor.ProcessAsync(404);

            Assert.Null(result);
            _orders.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task An_order_without_items_is_saved_without_touching_any_handler()
        {
            GivenTheOrder(42);

            Order? result = await _processor.ProcessAsync(42);

            Assert.NotNull(result);
            _resolver.Verify(r => r.Resolve(It.IsAny<ProductType>()), Times.Never);
            _orders.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private void GivenTheOrder(long orderId, params Product[] items)
        {
            Order order = new() { Id = orderId, Items = new HashSet<Product>(items) };
            _orders.Setup(o => o.FindWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(order);
        }

        private static Product AProduct(ProductType type) => new() { Type = type, Name = $"{type} product" };
    }
}

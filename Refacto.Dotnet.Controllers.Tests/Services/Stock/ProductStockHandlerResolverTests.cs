using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Stock;

namespace Refacto.DotNet.Controllers.Tests.Services.Stock
{
    public class ProductStockHandlerResolverTests
    {
        [Fact]
        public void Resolve_ReturnsTheHandlerMatchingTheRequestedType()
        {
            Mock<IProductStockHandler> normalHandler = new();
            _ = normalHandler.SetupGet(h => h.ProductType).Returns(ProductType.Normal);
            Mock<IProductStockHandler> seasonalHandler = new();
            _ = seasonalHandler.SetupGet(h => h.ProductType).Returns(ProductType.Seasonal);

            ProductStockHandlerResolver resolver = new(new[] { normalHandler.Object, seasonalHandler.Object });

            Assert.Same(seasonalHandler.Object, resolver.Resolve(ProductType.Seasonal));
        }

        [Fact]
        public void Resolve_WhenNoHandlerIsRegisteredForTheType_Throws()
        {
            ProductStockHandlerResolver resolver = new(Array.Empty<IProductStockHandler>());

            _ = Assert.Throws<NotSupportedException>(() => resolver.Resolve(ProductType.Expirable));
        }
    }
}

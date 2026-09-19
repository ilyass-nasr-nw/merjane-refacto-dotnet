using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.Dotnet.Controllers.Tests.Application.Products
{
    public class ProductHandlerResolverTests
    {
        [Theory]
        [InlineData(ProductType.Normal)]
        [InlineData(ProductType.Seasonal)]
        [InlineData(ProductType.Expirable)]
        public void It_returns_the_handler_that_declares_the_requested_type(ProductType type)
        {
            IProductHandler expected = new StubHandler(type);
            ProductHandlerResolver resolver = new(new[] { expected, new StubHandler(Other(type)) });

            Assert.Same(expected, resolver.Resolve(type));
        }

        [Fact]
        public void It_resolves_every_type_it_was_given_handlers_for()
        {
            ProductType[] allTypes = Enum.GetValues<ProductType>();
            ProductHandlerResolver resolver = new(allTypes.Select(t => new StubHandler(t)));

            foreach (ProductType type in allTypes)
            {
                Assert.Equal(type, resolver.Resolve(type).HandledType);
            }
        }

        [Fact]
        public void An_unregistered_type_fails_loudly_rather_than_silently_skipping_the_product()
        {
            ProductHandlerResolver resolver = new(new[] { new StubHandler(ProductType.Normal) });

            Assert.Throws<NotSupportedException>(() => resolver.Resolve(ProductType.Seasonal));
        }

        private static ProductType Other(ProductType type) =>
            type == ProductType.Normal ? ProductType.Expirable : ProductType.Normal;

        private sealed class StubHandler : IProductHandler
        {
            public StubHandler(ProductType handledType) => HandledType = handledType;

            public ProductType HandledType { get; }

            public void Handle(Product product)
            {
            }
        }
    }
}

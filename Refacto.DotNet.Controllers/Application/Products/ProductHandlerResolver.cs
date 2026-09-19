using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public sealed class ProductHandlerResolver : IProductHandlerResolver
    {
        private readonly IReadOnlyDictionary<ProductType, IProductHandler> _handlers;

        public ProductHandlerResolver(IEnumerable<IProductHandler> handlers)
        {
            _handlers = handlers.ToDictionary(handler => handler.HandledType);
        }

        public IProductHandler Resolve(ProductType type)
        {
            return _handlers.TryGetValue(type, out IProductHandler? handler)
                ? handler
                : throw new NotSupportedException($"No handler is registered for product type '{type}'.");
        }
    }
}

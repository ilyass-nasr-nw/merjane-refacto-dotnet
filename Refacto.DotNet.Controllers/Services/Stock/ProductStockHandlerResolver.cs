using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock
{
    /// <summary>
    /// Default resolver: builds a lookup table from every <see cref="IProductStockHandler"/>
    /// registered in the DI container, keyed by the product type it handles.
    /// Adding a new product type only requires registering a new handler
    /// (Open/Closed Principle) - no existing code needs to change.
    /// </summary>
    public class ProductStockHandlerResolver : IProductStockHandlerResolver
    {
        private readonly IReadOnlyDictionary<ProductType, IProductStockHandler> _handlersByType;

        public ProductStockHandlerResolver(IEnumerable<IProductStockHandler> handlers)
        {
            _handlersByType = handlers.ToDictionary(handler => handler.ProductType);
        }

        public IProductStockHandler Resolve(ProductType productType)
        {
            if (!_handlersByType.TryGetValue(productType, out IProductStockHandler? handler))
            {
                throw new NotSupportedException($"No stock handler is registered for product type '{productType}'.");
            }

            return handler;
        }
    }
}

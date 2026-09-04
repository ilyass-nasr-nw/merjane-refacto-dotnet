using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock
{
    /// <summary>
    /// Resolves the <see cref="IProductStockHandler"/> responsible for a given
    /// <see cref="ProductType"/>.
    /// </summary>
    public interface IProductStockHandlerResolver
    {
        IProductStockHandler Resolve(ProductType productType);
    }
}

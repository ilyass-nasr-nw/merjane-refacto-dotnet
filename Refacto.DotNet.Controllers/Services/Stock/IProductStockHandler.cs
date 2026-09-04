using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Stock
{
    /// <summary>
    /// Encapsulates the stock/availability rules for a single <see cref="ProductType"/>.
    /// Implementations only mutate the in-memory <see cref="Product"/> and trigger
    /// notifications; persistence is the caller's responsibility (Single Responsibility).
    /// </summary>
    public interface IProductStockHandler
    {
        ProductType ProductType { get; }

        void Process(Product product);
    }
}

using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public interface IProductHandlerResolver
    {
        IProductHandler Resolve(ProductType type);
    }
}

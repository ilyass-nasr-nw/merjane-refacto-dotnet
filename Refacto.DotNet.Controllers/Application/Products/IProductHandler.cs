using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Application.Products
{
    public interface IProductHandler
    {
        ProductType HandledType { get; }

        void Handle(Product product);
    }
}

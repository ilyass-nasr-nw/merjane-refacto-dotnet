namespace Refacto.DotNet.Controllers.Entities
{
    /// <summary>
    /// The commercial category of a product, which determines how its stock
    /// is replenished/consumed when an order is processed.
    /// </summary>
    public enum ProductType
    {
        Normal,
        Seasonal,
        Expirable
    }
}

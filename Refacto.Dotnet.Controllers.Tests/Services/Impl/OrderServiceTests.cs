using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Impl;
using Refacto.DotNet.Controllers.Services.Stock;

namespace Refacto.DotNet.Controllers.Tests.Services.Impl
{
    public class OrderServiceTests
    {
        private static AppDbContext CreateContext()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task ProcessOrderAsync_WhenOrderDoesNotExist_ReturnsNull()
        {
            using AppDbContext context = CreateContext();
            OrderService orderService = new(context, new ProductStockHandlerResolver(Array.Empty<IProductStockHandler>()));

            Order? result = await orderService.ProcessOrderAsync(orderId: 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task ProcessOrderAsync_DelegatesEachItemToItsHandlerAndPersistsChanges()
        {
            using AppDbContext context = CreateContext();

            Product normalProduct = new() { Type = ProductType.Normal, Available = 3, LeadTime = 5, Name = "Cable" };
            Order order = new() { Items = new List<Product> { normalProduct } };
            _ = context.Products.Add(normalProduct);
            _ = context.Orders.Add(order);
            _ = await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            RecordingStockHandler recordingHandler = new(ProductType.Normal);
            OrderService orderService = new(context, new ProductStockHandlerResolver(new IProductStockHandler[] { recordingHandler }));

            Order? result = await orderService.ProcessOrderAsync(order.Id);

            Assert.NotNull(result);
            Assert.True(recordingHandler.WasCalled);

            Product? persisted = await context.Products.FindAsync(normalProduct.Id);
            Assert.NotNull(persisted);
            Assert.Equal(2, persisted!.Available); // started at 3, RecordingStockHandler decrements by 1
        }

        /// <summary>Minimal test double used to verify that OrderService dispatches to the resolver.</summary>
        private sealed class RecordingStockHandler : IProductStockHandler
        {
            public RecordingStockHandler(ProductType productType) => ProductType = productType;

            public ProductType ProductType { get; }

            public bool WasCalled { get; private set; }

            public void Process(Product product)
            {
                WasCalled = true;
                product.Available -= 1;
            }
        }
    }
}

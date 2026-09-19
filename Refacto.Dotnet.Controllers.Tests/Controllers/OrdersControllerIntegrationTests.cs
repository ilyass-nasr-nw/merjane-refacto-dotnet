using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class OrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<INotificationService> _notifications = new();

        public OrdersControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"InMemoryDbForTesting-{nameof(OrdersControllerIntegrationTests)}"));

                    services.RemoveAll<INotificationService>();
                    services.AddScoped(_ => _notifications.Object);
                }));

            using AppDbContext context = NewContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Normal_product_in_stock_is_decremented_and_triggers_no_notification()
        {
            Product product = new() { LeadTime = 15, Available = 30, Type = ProductType.Normal, Name = "USB Cable" };

            await ProcessOrderContaining(product);

            Assert.Equal(29, Reload(product).Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Normal_product_out_of_stock_announces_the_lead_time()
        {
            Product product = new() { LeadTime = 10, Available = 0, Type = ProductType.Normal, Name = "USB Dongle" };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Normal_product_out_of_stock_without_lead_time_is_silently_ignored()
        {
            Product product = new() { LeadTime = 0, Available = 0, Type = ProductType.Normal, Name = "Ethernet Cable" };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_product_in_season_and_in_stock_is_decremented()
        {
            Product product = new()
            {
                LeadTime = 15,
                Available = 30,
                Type = ProductType.Seasonal,
                Name = "Watermelon",
                SeasonStartDate = DateTime.Now.AddDays(-2),
                SeasonEndDate = DateTime.Now.AddDays(58)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(29, Reload(product).Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_product_whose_season_has_not_started_is_reported_unavailable_but_keeps_its_stock()
        {
            Product product = new()
            {
                LeadTime = 15,
                Available = 30,
                Type = ProductType.Seasonal,
                Name = "Grapes",
                SeasonStartDate = DateTime.Now.AddDays(180),
                SeasonEndDate = DateTime.Now.AddDays(240)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(30, Reload(product).Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_product_in_season_but_out_of_stock_announces_the_lead_time()
        {
            Product product = new()
            {
                LeadTime = 5,
                Available = 0,
                Type = ProductType.Seasonal,
                Name = "Strawberry",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(40)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendDelayNotification(5, "Strawberry"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_product_whose_restock_lands_after_the_season_is_marked_unavailable()
        {
            Product product = new()
            {
                LeadTime = 30,
                Available = 0,
                Type = ProductType.Seasonal,
                Name = "Cherry",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(5)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Cherry"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_restock_overshooting_the_season_takes_precedence_over_a_season_not_started()
        {
            Product product = new()
            {
                LeadTime = 120,
                Available = 30,
                Type = ProductType.Seasonal,
                Name = "Pumpkin",
                SeasonStartDate = DateTime.Now.AddDays(30),
                SeasonEndDate = DateTime.Now.AddDays(90)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendOutOfStockNotification("Pumpkin"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Seasonal_product_out_of_stock_without_lead_time_stays_silent()
        {
            Product product = new()
            {
                LeadTime = 0,
                Available = 0,
                Type = ProductType.Seasonal,
                Name = "Fig",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(40)
            };

            await ProcessOrderContaining(product);

            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expirable_product_still_fresh_and_in_stock_is_decremented()
        {
            Product product = new()
            {
                LeadTime = 15,
                Available = 30,
                Type = ProductType.Expirable,
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(26)
            };

            await ProcessOrderContaining(product);

            Assert.Equal(29, Reload(product).Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expired_product_is_reported_expired_and_its_stock_is_zeroed()
        {
            DateTime expiry = DateTime.Now.AddDays(-2);
            Product product = new()
            {
                LeadTime = 90,
                Available = 6,
                Type = ProductType.Expirable,
                Name = "Milk",
                ExpiryDate = expiry
            };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendExpirationNotification("Milk", expiry), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Fresh_expirable_product_that_is_merely_out_of_stock_announces_a_delay()
        {
            DateTime expiry = DateTime.Now.AddDays(30);
            Product product = new()
            {
                LeadTime = 7,
                Available = 0,
                Type = ProductType.Expirable,
                Name = "Yoghurt",
                ExpiryDate = expiry
            };

            await ProcessOrderContaining(product);

            Assert.Equal(0, Reload(product).Available);
            _notifications.Verify(n => n.SendDelayNotification(7, "Yoghurt"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expirable_product_without_an_expiry_date_never_expires()
        {
            Product product = new()
            {
                LeadTime = 3,
                Available = 4,
                Type = ProductType.Expirable,
                Name = "Salt",
                ExpiryDate = null
            };

            await ProcessOrderContaining(product);

            Assert.Equal(3, Reload(product).Available);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task A_mixed_order_applies_the_right_rule_to_every_single_item()
        {
            DateTime milkExpiry = DateTime.Now.AddDays(-2);
            Product usbCable = new() { LeadTime = 15, Available = 30, Type = ProductType.Normal, Name = "USB Cable" };
            Product usbDongle = new() { LeadTime = 10, Available = 0, Type = ProductType.Normal, Name = "USB Dongle" };
            Product butter = new() { LeadTime = 15, Available = 30, Type = ProductType.Expirable, Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) };
            Product milk = new() { LeadTime = 90, Available = 6, Type = ProductType.Expirable, Name = "Milk", ExpiryDate = milkExpiry };
            Product watermelon = new() { LeadTime = 15, Available = 30, Type = ProductType.Seasonal, Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) };
            Product grapes = new() { LeadTime = 15, Available = 30, Type = ProductType.Seasonal, Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) };

            await ProcessOrderContaining(usbCable, usbDongle, butter, milk, watermelon, grapes);

            Assert.Equal(29, Reload(usbCable).Available);
            Assert.Equal(0, Reload(usbDongle).Available);
            Assert.Equal(29, Reload(butter).Available);
            Assert.Equal(0, Reload(milk).Available);
            Assert.Equal(29, Reload(watermelon).Available);
            Assert.Equal(30, Reload(grapes).Available);

            _notifications.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once);
            _notifications.Verify(n => n.SendExpirationNotification("Milk", milkExpiry), Times.Once);
            _notifications.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once);
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task An_order_without_any_item_succeeds_and_notifies_nobody()
        {
            long orderId = await SeedOrder();

            HttpResponseMessage response = await _factory.CreateClient().PostAsync($"/orders/{orderId}/processOrder", null);

            response.EnsureSuccessStatusCode();
            _notifications.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task An_unknown_order_returns_not_found()
        {
            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.PostAsync("/orders/404404/processOrder", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public void The_container_registers_a_handler_for_every_product_type()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            IProductHandlerResolver resolver = scope.ServiceProvider.GetRequiredService<IProductHandlerResolver>();

            foreach (ProductType type in Enum.GetValues<ProductType>())
            {
                Assert.Equal(type, resolver.Resolve(type).HandledType);
            }
        }

        private AppDbContext NewContext() =>
            _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

        private async Task<long> SeedOrder(params Product[] products)
        {
            using AppDbContext context = NewContext();
            Order order = new() { Items = new HashSet<Product>(products) };
            await context.Products.AddRangeAsync(products);
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();
            return order.Id;
        }

        private async Task ProcessOrderContaining(params Product[] products)
        {
            long orderId = await SeedOrder(products);

            HttpResponseMessage response = await _factory.CreateClient()
                .PostAsync($"/orders/{orderId}/processOrder", null);

            response.EnsureSuccessStatusCode();
        }

        private Product Reload(Product product)
        {
            using AppDbContext context = NewContext();
            return context.Products.AsNoTracking().Single(p => p.Id == product.Id);
        }
    }
}

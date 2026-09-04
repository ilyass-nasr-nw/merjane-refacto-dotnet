using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class OrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public OrdersControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);


                    ServiceProvider sp = services.BuildServiceProvider();
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrder_WhenOrderExists_AppliesBusinessRulesForEveryProductType()
        {
            HttpClient client = _factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            Order? resultOrder = await _context.Orders.FindAsync(order.Id);
            Assert.Equal(resultOrder!.Id, order.Id);

            Product usbCable = await GetProductByName("USB Cable");
            Assert.Equal(29, usbCable.Available); // NORMAL, was in stock -> decremented

            Product usbDongle = await GetProductByName("USB Dongle");
            Assert.Equal(0, usbDongle.Available); // NORMAL, out of stock -> delay notified
            _mockNotificationService.Verify(s => s.SendDelayNotification(10, "USB Dongle"), Times.Once());

            Product butter = await GetProductByName("Butter");
            Assert.Equal(29, butter.Available); // EXPIRABLE, not expired -> decremented

            Product milk = await GetProductByName("Milk");
            Assert.Equal(0, milk.Available); // EXPIRABLE, expired -> unavailable + notified
            _mockNotificationService.Verify(
                s => s.SendExpirationNotification("Milk", It.IsAny<DateTime>()), Times.Once());

            Product watermelon = await GetProductByName("Watermelon");
            Assert.Equal(29, watermelon.Available); // SEASONAL, in season & in stock -> decremented

            Product grapes = await GetProductByName("Grapes");
            Assert.Equal(30, grapes.Available); // SEASONAL, season not started -> untouched
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification("Grapes"), Times.Once());
        }

        [Fact]
        public async Task ProcessOrder_WhenOrderDoesNotExist_ReturnsNotFound()
        {
            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.PostAsync("/orders/999999/processOrder", null);

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        private async Task<Product> GetProductByName(string name)
        {
            return await _context.Products.SingleAsync(p => p.Name == name);
        }

        private static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }

        private static List<Product> CreateProducts()
        {
            return new List<Product>
            {
                new Product { LeadTime = 15, Available = 30, Type = ProductType.Normal, Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = ProductType.Normal, Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = ProductType.Expirable, Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = ProductType.Expirable, Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = ProductType.Seasonal, Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = ProductType.Seasonal, Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
            };
        }
    }
}

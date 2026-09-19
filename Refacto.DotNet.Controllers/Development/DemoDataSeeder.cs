using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Infrastructure.Time;

namespace Refacto.DotNet.Controllers.Development
{
    public sealed record DemoCase(long OrderId, string Scenario, string Expected);

    public sealed class DemoDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _clock;

        public DemoDataSeeder(AppDbContext context, IDateTimeProvider clock)
        {
            _context = context;
            _clock = clock;
        }

        public IReadOnlyList<DemoCase> Seed()
        {
            _context.Orders.RemoveRange(_context.Orders);
            _context.Products.RemoveRange(_context.Products);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            DateTime today = _clock.Today;
            List<DemoCase> cases = new();
            long productId = 1;

            void Add(long orderId, string scenario, string expected, params Product[] products)
            {
                foreach (Product product in products)
                {
                    product.Id = productId++;
                }

                _context.Products.AddRange(products);
                _context.Orders.Add(new Order { Id = orderId, Items = new HashSet<Product>(products) });
                cases.Add(new DemoCase(orderId, scenario, expected));
            }

            Add(1, "NORMAL in stock",
                "available 30 -> 29, no notification",
                Normal("USB Cable", available: 30, leadTime: 15));

            Add(2, "NORMAL out of stock, lead time 10",
                "available stays 0, DELAY notification (10 days)",
                Normal("USB Dongle", available: 0, leadTime: 10));

            Add(3, "NORMAL out of stock, no lead time",
                "available stays 0, NO notification",
                Normal("Ethernet Cable", available: 0, leadTime: 0));

            Add(4, "SEASONAL in season, in stock",
                "available 30 -> 29, no notification",
                Seasonal("Watermelon", available: 30, leadTime: 15, start: today.AddDays(-2), end: today.AddDays(58)));

            Add(5, "SEASONAL season has not started",
                "available STAYS 30, OUT_OF_STOCK notification",
                Seasonal("Grapes", available: 30, leadTime: 15, start: today.AddDays(180), end: today.AddDays(240)));

            Add(6, "SEASONAL in season, out of stock, restock arrives in time",
                "available stays 0, DELAY notification (5 days)",
                Seasonal("Strawberry", available: 0, leadTime: 5, start: today.AddDays(-10), end: today.AddDays(40)));

            Add(7, "SEASONAL restock lands after the season ends",
                "available -> 0, OUT_OF_STOCK notification",
                Seasonal("Cherry", available: 0, leadTime: 30, start: today.AddDays(-10), end: today.AddDays(5)));

            Add(8, "SEASONAL on the FIRST day of its season (inclusive bound)",
                "available 30 -> 29, no notification",
                Seasonal("Fig", available: 30, leadTime: 15, start: today, end: today.AddDays(40)));

            Add(9, "EXPIRABLE fresh, in stock",
                "available 30 -> 29, no notification",
                Expirable("Butter", available: 30, leadTime: 15, expiry: today.AddDays(26)));

            Add(10, "EXPIRABLE expired",
                "available -> 0, EXPIRED notification",
                Expirable("Milk", available: 6, leadTime: 90, expiry: today.AddDays(-2)));

            Add(11, "EXPIRABLE fresh but out of stock (the main correction)",
                "DELAY notification (7 days), NOT an expiration",
                Expirable("Yoghurt", available: 0, leadTime: 7, expiry: today.AddDays(30)));

            Add(12, "EXPIRABLE on its expiry date (inclusive bound)",
                "available 6 -> 5, no notification",
                Expirable("Cheese", available: 6, leadTime: 20, expiry: today));

            Add(13, "EXPIRABLE with no expiry date",
                "available 4 -> 3, no notification (used to crash)",
                Expirable("Salt", available: 4, leadTime: 3, expiry: null));

            Add(14, "MIXED order, the six products of the brief",
                "29 / 0 + DELAY / 29 / 0 + EXPIRED / 29 / 30 + OUT_OF_STOCK",
                Normal("USB Cable (mixed)", available: 30, leadTime: 15),
                Normal("USB Dongle (mixed)", available: 0, leadTime: 10),
                Expirable("Butter (mixed)", available: 30, leadTime: 15, expiry: today.AddDays(26)),
                Expirable("Milk (mixed)", available: 6, leadTime: 90, expiry: today.AddDays(-2)),
                Seasonal("Watermelon (mixed)", available: 30, leadTime: 15, start: today.AddDays(-2), end: today.AddDays(58)),
                Seasonal("Grapes (mixed)", available: 30, leadTime: 15, start: today.AddDays(180), end: today.AddDays(240)));

            Add(15, "EMPTY order", "HTTP 200, no notification");

            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            cases.Add(new DemoCase(999, "UNKNOWN order", "HTTP 404"));
            return cases;
        }

        private static Product Normal(string name, int available, int leadTime) => new()
        {
            Type = ProductType.Normal, Name = name, Available = available, LeadTime = leadTime
        };

        private static Product Seasonal(string name, int available, int leadTime, DateTime start, DateTime end) => new()
        {
            Type = ProductType.Seasonal, Name = name, Available = available, LeadTime = leadTime,
            SeasonStartDate = start, SeasonEndDate = end
        };

        private static Product Expirable(string name, int available, int leadTime, DateTime? expiry) => new()
        {
            Type = ProductType.Expirable, Name = name, Available = available, LeadTime = leadTime,
            ExpiryDate = expiry
        };
    }
}

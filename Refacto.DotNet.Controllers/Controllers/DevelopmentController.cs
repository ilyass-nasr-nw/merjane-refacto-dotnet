using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Development;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("dev")]
    public class DevelopmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DemoDataSeeder _seeder;
        private readonly NotificationRecorder _recorder;
        private readonly IWebHostEnvironment _environment;

        public DevelopmentController(
            AppDbContext context,
            DemoDataSeeder seeder,
            NotificationRecorder recorder,
            IWebHostEnvironment environment)
        {
            _context = context;
            _seeder = seeder;
            _recorder = recorder;
            _environment = environment;
        }

        [HttpPost("seed")]
        public ActionResult<IReadOnlyList<DemoCase>> Seed()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _recorder.Clear();
            return Ok(_seeder.Seed());
        }

        [HttpGet("products")]
        public async Task<ActionResult<object>> Products(CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            List<Product> products = await _context.Products
                .AsNoTracking()
                .OrderBy(product => product.Id)
                .ToListAsync(cancellationToken);

            return Ok(products.Select(product => new
            {
                product.Id,
                product.Name,
                Type = product.Type.ToString(),
                product.Available,
                product.LeadTime,
                ExpiryDate = product.ExpiryDate?.ToString("yyyy-MM-dd"),
                SeasonStartDate = product.SeasonStartDate?.ToString("yyyy-MM-dd"),
                SeasonEndDate = product.SeasonEndDate?.ToString("yyyy-MM-dd")
            }));
        }

        [HttpGet("notifications")]
        public ActionResult<IReadOnlyList<string>> Notifications()
        {
            return !_environment.IsDevelopment() ? NotFound() : Ok(_recorder.Entries);
        }

        [HttpDelete("notifications")]
        public IActionResult ClearNotifications()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _recorder.Clear();
            return NoContent();
        }
    }
}

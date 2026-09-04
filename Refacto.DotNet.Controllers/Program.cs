using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;
using Refacto.DotNet.Controllers.Services.Stock;
using Refacto.DotNet.Controllers.Services.Stock.Handlers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockHandlerResolver, ProductStockHandlerResolver>();
builder.Services.AddScoped<IProductStockHandler, NormalProductStockHandler>();
builder.Services.AddScoped<IProductStockHandler, SeasonalProductStockHandler>();
builder.Services.AddScoped<IProductStockHandler, ExpirableProductStockHandler>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseInMemoryDatabase($"InMemoryDb");
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Application.Orders;
using Refacto.DotNet.Controllers.Application.Products;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Infrastructure.Persistence;
using Refacto.DotNet.Controllers.Infrastructure.Time;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<IProductHandler, NormalProductHandler>();
builder.Services.AddScoped<IProductHandler, SeasonalProductHandler>();
builder.Services.AddScoped<IProductHandler, ExpirableProductHandler>();
builder.Services.AddScoped<IProductHandlerResolver, ProductHandlerResolver>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();
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
using Microsoft.EntityFrameworkCore;
using Shop.Features.Orders;
using Shop.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Shop") ?? "Data Source=shop.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOrdersFeature();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOrders();

app.Run();

public partial class Program;

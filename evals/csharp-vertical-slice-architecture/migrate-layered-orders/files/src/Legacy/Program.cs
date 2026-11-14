using Legacy.Data;
using Legacy.Infrastructure;
using Legacy.Repositories;
using Legacy.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<LegacyDbContext>(o => o.UseSqlite("Data Source=legacy.db"));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMappingMiddleware>();
app.MapControllers();

app.Run();

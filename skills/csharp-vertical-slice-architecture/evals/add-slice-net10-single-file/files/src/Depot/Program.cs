using Depot.Features.Shipments;
using Depot.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DepotDbContext>(o => o.UseSqlite("Data Source=depot.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddShipments();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapShipments();

app.Run();

public partial class Program;

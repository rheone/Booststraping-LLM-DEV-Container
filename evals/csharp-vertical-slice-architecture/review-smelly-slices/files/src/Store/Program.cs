using Microsoft.EntityFrameworkCore;
using Store.Features.Billing.CreateInvoice;
using Store.Features.Orders.CancelOrder;
using Store.Features.Orders.CreateOrder;
using Store.Features.Orders.GetOrderById;
using Store.Features.Orders.ListOrders;
using Store.Features.Orders.Reorder;
using Store.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StoreDbContext>(o => o.UseSqlite("Data Source=store.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<CancelOrderHandler>();
builder.Services.AddScoped<ReorderHandler>();
builder.Services.AddScoped<ListOrdersHandler>();
builder.Services.AddScoped<CreateInvoiceHandler>();

var app = builder.Build();

app.UseExceptionHandler();

RouteGroupBuilder orders = app.MapGroup("/orders");
CreateOrderEndpoint.Map(orders);
CancelOrderEndpoint.Map(orders);
ReorderEndpoint.Map(orders);
GetOrderByIdEndpoint.Map(orders);
ListOrdersEndpoint.Map(orders);
CreateInvoiceEndpoint.Map(app.MapGroup("/invoices"));

app.Run();

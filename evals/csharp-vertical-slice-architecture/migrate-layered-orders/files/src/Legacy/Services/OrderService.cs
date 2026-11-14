using Legacy.Infrastructure;
using Legacy.Models;
using Legacy.Repositories;

namespace Legacy.Services;

public interface IOrderService
{
    Task<OrderDto> GetAsync(int id);

    Task<OrderDto> CreateAsync(CreateOrderDto dto);

    Task<OrderDto> ShipAsync(int id);
}

public class OrderService(IRepository<Order> orders) : IOrderService
{
    public async Task<OrderDto> GetAsync(int id)
    {
        Order order = await orders.GetByIdAsync(id) ?? throw new NotFoundException($"Order {id} not found");
        return Map(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            throw new BadRequestException("CustomerName is required");
        }

        if (dto.Total <= 0)
        {
            throw new BadRequestException("Total must be positive");
        }

        Order order = new() { CustomerName = dto.CustomerName, Total = dto.Total, CreatedAt = DateTime.UtcNow };
        await orders.AddAsync(order);
        await orders.SaveAsync();
        return Map(order);
    }

    public async Task<OrderDto> ShipAsync(int id)
    {
        Order order = await orders.GetByIdAsync(id) ?? throw new NotFoundException($"Order {id} not found");
        if (order.Status == "Shipped")
        {
            throw new ConflictException($"Order {id} already shipped");
        }

        order.Status = "Shipped";
        order.ShippedAt = DateTime.UtcNow;
        await orders.SaveAsync();
        return Map(order);
    }

    private static OrderDto Map(Order order) => new()
    {
        Id = order.Id,
        CustomerName = order.CustomerName,
        Status = order.Status,
        Total = order.Total,
        ShippedAt = order.ShippedAt,
    };
}

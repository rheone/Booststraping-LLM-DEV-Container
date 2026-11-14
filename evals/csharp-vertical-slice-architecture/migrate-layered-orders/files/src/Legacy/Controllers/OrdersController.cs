using Legacy.Models;
using Legacy.Services;
using Microsoft.AspNetCore.Mvc;

namespace Legacy.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id) => Ok(await orderService.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
    {
        OrderDto created = await orderService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id) => Ok(await orderService.ShipAsync(id));
}

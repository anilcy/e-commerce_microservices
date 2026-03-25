using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.Features.Orders.Commands;
using Order.API.Features.Orders.Queries;
using System.Security.Claims;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Place a new order.</summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var command = new PlaceOrderCommand(CurrentUserId, request.ShippingAddress, request.Items);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    /// <summary>Get all orders for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var result = await _mediator.Send(new GetOrdersByUserQuery(CurrentUserId));
        return Ok(result);
    }

    /// <summary>Get a specific order by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id, CurrentUserId));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Cancel an order.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, CurrentUserId));
        return result ? NoContent() : NotFound();
    }
}

// Request DTO (separate from command to avoid coupling controller inputs to CQRS layer)
public record PlaceOrderRequest(
    string? ShippingAddress,
    List<Features.Orders.Commands.OrderItemRequest> Items);

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.DTO;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Domain.DTO;

namespace OrderService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            if (command == null || !command.ProductItems.Any())
            {
                _logger.LogWarning("CreateOrder called with invalid command data.");
                return BadRequest("Order command is invalid or has no items.");
            }

            _logger.LogInformation("Received request to create order for customer {CustomerId} with {ItemCount} items.",
                command.CustomerId, command.ProductItems.Count);

            try
            {
                var orderId = await _mediator.Send(command);
                _logger.LogInformation("Order {OrderId} created successfully.", orderId);
                return CreatedAtAction(nameof(GetOrderById), new { orderId = orderId }, orderId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating order.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating order for customer {CustomerId}.", command.CustomerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            _logger.LogInformation("Received request to get order by ID: {OrderId}", orderId);
            var order = await _mediator.Send(new GetOrderByIdQuery(orderId));
            if (order == null) return NotFound(new { Message = $"Order with ID {orderId} not found." });
            return Ok(order);
        }

        [HttpPut("{orderId:guid}/process")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessOrder(Guid orderId)
        {
            _logger.LogInformation("Received request to process order {OrderId}", orderId);
            var command = new ProcessOrderCommand(orderId);
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { Message = $"Order {orderId} not found or could not be processed." });
            return Ok(new { Message = $"Order {orderId} processed successfully." });
        }

        [HttpPut("{orderId:guid}/ship")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ShipOrder(Guid orderId, [FromBody] ShipOrderRequestPayload? payload)
        {
            _logger.LogInformation("Received request to ship order {OrderId}", orderId);
            var command = new ShipOrderCommand(orderId, payload?.TrackingNumber);
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { Message = $"Order {orderId} not found or could not be shipped." }); // Could also be BadRequest if status invalid
            return Ok(new { Message = $"Order {orderId} shipped successfully. Tracking: {payload?.TrackingNumber ?? "N/A"}" });
        }

        [HttpPut("{orderId:guid}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteOrder(Guid orderId)
        {
            _logger.LogInformation("Received request to complete order {OrderId}", orderId);
            var command = new CompleteOrderCommand(orderId);
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { Message = $"Order {orderId} not found or could not be completed." });
            return Ok(new { Message = $"Order {orderId} completed successfully." });
        }

        [HttpPut("{orderId:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequestPayload? payload)
        {
            _logger.LogInformation("Received request to cancel order {OrderId}", orderId);
            var command = new CancelOrderCommand(orderId, payload?.Reason);
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { Message = $"Order {orderId} not found or could not be cancelled." });
            return Ok(new { Message = $"Order {orderId} cancelled successfully. Reason: {payload?.Reason ?? "N/A"}" });
        }
    }
}
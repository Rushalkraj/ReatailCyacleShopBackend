using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.models.enums;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.Services;
using RetailCycleShopAPI.Interfaces;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IInventoryTracker _inventoryTracker;

        public OrderController(
            ApplicationDbContext context,
            ILogger<OrderController> logger,
            IInventoryTracker inventoryTracker)
        {
            _context = context;
            _logger = logger;
            _inventoryTracker = inventoryTracker;
        }

        [HttpPost("customer")]
        public async Task<ActionResult<Order>> CreateCustomerOrder([FromBody] OrderCreateDto orderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate input
                if (orderDto == null) return BadRequest("Order data is required");
                if (orderDto.Items == null || !orderDto.Items.Any())
                    return BadRequest("Order must contain items");

                // Validate customer
                var customer = await _context.Customers
                    .Include(c => c.ShippingAddress)
                    .FirstOrDefaultAsync(c => c.CustomerId == orderDto.CustomerId);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", orderDto.CustomerId);
                    return BadRequest("Customer not found");
                }

                // Create order
                var order = new Order
                {
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(10000, 99999)}",
                    CustomerId = orderDto.CustomerId,
                    ShippingAddressId = orderDto.ShippingAddressId,
                    OrderDate = DateTime.UtcNow,
                    Status = (int)OrderStatus.Pending,
                    Subtotal = orderDto.Subtotal,
                    Tax = orderDto.Tax,
                    TotalAmount = orderDto.TotalAmount,
                    Notes = orderDto.PaymentMethod,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get OrderId

                // Process items
                foreach (var item in orderDto.Items)
                {
                    var cycle = await _context.Cycles
                        .Include(c => c.Inventory)
                        .FirstOrDefaultAsync(c => c.CycleId == item.CycleId);

                    if (cycle == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Cycle {item.CycleId} not found");
                    }

                    if (cycle.Inventory == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Inventory record missing for cycle {item.CycleId}");
                    }

                    if (cycle.Inventory.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Insufficient stock for {cycle.Brand} {cycle.Model}");
                    }

                    // Update stock
                    var oldQuantity = cycle.Inventory.StockQuantity;
                    cycle.Inventory.StockQuantity -= item.Quantity;
                    cycle.StockQuantity = cycle.Inventory.StockQuantity;

                    // Track change
                    await _inventoryTracker.TrackChangeAsync(
                        cycle.CycleId,
                        oldQuantity,
                        cycle.Inventory.StockQuantity,
                        $"Order #{order.OrderId}",
                        order.OrderId);

                    // Add order item
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        CycleId = cycle.CycleId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.UnitPrice * item.Quantity
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Order creation failed");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            return order == null ? NotFound() : order;
        }
        [HttpPut("{orderId}/payment")]
        public async Task<IActionResult> UpdateOrderPayment(int orderId, [FromBody] PaymentUpdateDto paymentDto)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                order.PaymentId = paymentDto.PaymentId;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order payment");
                return StatusCode(500, "Internal server error");
            }
        }

      
        [HttpPatch("{orderId}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] StatusUpdateDto statusDto)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = statusDto.Status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderItems)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Restore inventory for each item
                foreach (var item in order.OrderItems)
                {
                    var cycle = await _context.Cycles
                        .Include(c => c.Inventory)
                        .FirstOrDefaultAsync(c => c.CycleId == item.CycleId);

                    if (cycle != null && cycle.Inventory != null)
                    {
                        var oldQuantity = cycle.Inventory.StockQuantity;
                        cycle.Inventory.StockQuantity += item.Quantity;
                        cycle.StockQuantity = cycle.Inventory.StockQuantity;

                        await _inventoryTracker.TrackChangeAsync(
                            cycle.CycleId,
                            oldQuantity,
                            cycle.Inventory.StockQuantity,
                            $"Order #{order.OrderId} deleted",
                            order.OrderId);
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting order");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}

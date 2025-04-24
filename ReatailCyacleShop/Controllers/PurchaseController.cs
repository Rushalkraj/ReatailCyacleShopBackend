using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Models;
using System;
using System.Threading.Tasks;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")] 
    public class PurchaseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PurchaseCycle([FromBody] PurchaseModel model)
        {
            if (model == null || model.CycleId <= 0 || model.CustomerId <= 0 || model.Quantity <= 0)
            {
                return BadRequest("Invalid purchase data.");
            }

            var cycle = await _context.Cycles.FindAsync(model.CycleId);
            if (cycle == null || cycle.StockQuantity < model.Quantity)
            {
                return BadRequest("Cycle not available or insufficient stock.");
            }

            // Deduct stock
            cycle.StockQuantity -= model.Quantity;
            _context.Cycles.Update(cycle);

            // Create an order
            var order = new Order
            {
                CustomerId = model.CustomerId,
                ShippingAddressId = 1, 
                OrderDate = DateTime.UtcNow,
                Status = (int)OrderStatus.Pending,
                Subtotal = cycle.Price * model.Quantity,
                Tax = cycle.Price * model.Quantity * 0.0625m,
                TotalAmount = cycle.Price * model.Quantity * 1.0625m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems =
       [
           new OrderItem
            {
                CycleId = model.CycleId,
                Quantity = model.Quantity,
                UnitPrice = cycle.Price,
                TaxRate = 0.0625m,
                TotalPrice = cycle.Price * model.Quantity * 1.0625m,
                CreatedAt = DateTime.UtcNow
            }
       ]
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Purchase successful!", OrderId = order.OrderId });
        }
    }

    public class PurchaseModel
    {
        public int CycleId { get; set; }
        public int CustomerId { get; set; }
        public int Quantity { get; set; }
    }
}
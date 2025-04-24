using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Interfaces;
using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.Services;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryTracker _inventoryTracker;

        public InventoryController(
            ApplicationDbContext context,
            IInventoryTracker inventoryTracker)
        {
            _context = context;
            _inventoryTracker = inventoryTracker;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventory()
        {
            return await _context.Inventories
                .Include(i => i.Cycle)
                .ToListAsync();
        }
        // In your controller or service:
        [HttpGet("{id}")]
        public async Task<ActionResult<Cycle>> GetCycle(int id)
        {
            var cycle = await _context.Cycles
                .Include(c => c.Inventory) // THIS IS CRUCIAL
                .FirstOrDefaultAsync(c => c.CycleId == id);

            if (cycle == null)
            {
                return NotFound();
            }

            return cycle;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateDto updateDto)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null) return NotFound();

            var oldQuantity = inventory.StockQuantity;
            inventory.StockQuantity = updateDto.NewQuantity;
            inventory.LastUpdated = DateTime.UtcNow;

            await _inventoryTracker.TrackChangeAsync(
                inventory.CycleId,
                oldQuantity,
                inventory.StockQuantity,
                updateDto.Reason ?? "Manual adjustment");

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

  
}
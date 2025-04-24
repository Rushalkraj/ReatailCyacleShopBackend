using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Interfaces;
using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.module;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class CyclesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryTracker _inventoryTracker;
        private readonly ILogger<CyclesController> _logger;

        public CyclesController(
          ApplicationDbContext context,
          IInventoryTracker inventoryTracker,
          ILogger<CyclesController> logger)
        {
            _context = context;
            _inventoryTracker = inventoryTracker;
            _logger = logger;
        }

        // GET: api/Cycles
        [HttpGet]
        
        public async Task<ActionResult<IEnumerable<Cycle>>> GetCycles()
        {

            return await _context.Cycles
               .Include(c => c.Inventory)
               .ToListAsync();
        }

        // GET: api/Cycles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cycle>> GetCycle(int id)
        {

            var cycle = await _context.Cycles
                 .Include(c => c.Inventory)
                 .FirstOrDefaultAsync(c => c.CycleId == id);

            return cycle == null ? NotFound() : cycle;
        }

        [HttpPost]
        public async Task<ActionResult<Cycle>> CreateCycle([FromBody] CycleCreateDto cycleDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (cycleDto == null) return BadRequest("Cycle data is required");
                if (cycleDto.StockQuantity < 0) return BadRequest("Stock quantity cannot be negative");

                // Create cycle
                var cycle = new Cycle
                {
                    Brand = cycleDto.Brand,
                    Type = cycleDto.Type,
                    Model = cycleDto.Model,
                    Price = cycleDto.Price,
                    StockQuantity = cycleDto.StockQuantity,
                    ImageUrl = cycleDto.ImageUrl
                };

                _context.Cycles.Add(cycle);
                await _context.SaveChangesAsync();

                // Create inventory
                var inventory = new Inventory
                {
                    CycleId = cycle.CycleId,
                    StockQuantity = cycle.StockQuantity,
                    LastUpdated = DateTime.UtcNow
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();

                // Track inventory change
                await _inventoryTracker.TrackChangeAsync(
                    cycle.CycleId,
                    0,
                    cycle.StockQuantity,
                    "Initial stock creation");

                await transaction.CommitAsync();

                // Return the complete cycle with inventory
                var result = await _context.Cycles
                    .Include(c => c.Inventory)
                    .FirstOrDefaultAsync(c => c.CycleId == cycle.CycleId);

                return CreatedAtAction(nameof(GetCycle), new { id = cycle.CycleId }, result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create cycle");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Details = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCycle(int id, [FromBody] CycleUpdateDto cycleDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingCycle = await _context.Cycles
                    .Include(c => c.Inventory)
                    .FirstOrDefaultAsync(c => c.CycleId == id);

                if (existingCycle == null) return NotFound();

                // Track stock changes if quantity was modified
                if (existingCycle.StockQuantity != cycleDto.StockQuantity)
                {
                    var oldQuantity = existingCycle.StockQuantity;
                    existingCycle.StockQuantity = cycleDto.StockQuantity;
                    existingCycle.Inventory.StockQuantity = cycleDto.StockQuantity;
                    existingCycle.Inventory.LastUpdated = DateTime.UtcNow;

                    await _inventoryTracker.TrackChangeAsync(
                        existingCycle.CycleId,
                        oldQuantity,
                        cycleDto.StockQuantity,
                        "Manual stock adjustment");
                }

                // Update other properties
                existingCycle.Brand = cycleDto.Brand;
                existingCycle.Type = cycleDto.Type;
                existingCycle.Model = cycleDto.Model;
                existingCycle.Price = cycleDto.Price;
                existingCycle.ImageUrl = cycleDto.ImageUrl;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update cycle");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Details = ex.Message
                });
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCycle(int id)
        {
            var cycle = await _context.Cycles.FindAsync(id);
            if (cycle == null) return NotFound();

            _context.Cycles.Remove(cycle);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CycleExists(int id) => _context.Cycles.Any(e => e.CycleId == id);
    }
}
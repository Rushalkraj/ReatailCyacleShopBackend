using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCycleShopAPI.module;
using Microsoft.EntityFrameworkCore;
namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class InventoryHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryHistoryController> _logger;

        public InventoryHistoryController(
             ApplicationDbContext context,
             ILogger<InventoryHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet("summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            try
            {
                var threshold = 5; 
                var days = 7; 

                var summary = new
                {
                    // Critical low stock (<= 2 items)
                    outOfStock = await _context.Cycles
                        .Where(c => c.StockQuantity < 1)
                        .CountAsync(),

                    // Low stock alerts
                    LowStock = await _context.Cycles
                        .Where(c => c.StockQuantity > 2 && c.StockQuantity <= threshold)
                        .CountAsync(),

                    // Recent inventory changes
                    RecentActivity = await _context.InventoryHistories
                        .Where(ih => ih.CreatedAt >= DateTime.UtcNow.AddDays(-days))
                        .Include(ih => ih.Cycle)
                        .OrderByDescending(ih => ih.CreatedAt)
                        .Take(10)
                        .Select(ih => new
                        {
                            Cycle = $"{ih.Cycle.Brand} {ih.Cycle.Model}",
                            Type = ih.NewQuantity > ih.PreviousQuantity ? "Restock" : "Reduction",
                            Change = Math.Abs(ih.NewQuantity - ih.PreviousQuantity),
                            ih.PreviousQuantity,
                            ih.NewQuantity,
                            ih.ChangeReason,
                            ih.CreatedAt
                        })
                        .ToListAsync(),

                    // Inventory change trends
                    WeeklyChanges = await _context.InventoryHistories
                        .Where(ih => ih.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                        .GroupBy(ih => ih.CreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Restocks = g.Count(ih => ih.NewQuantity > ih.PreviousQuantity),
                            Reductions = g.Count(ih => ih.NewQuantity < ih.PreviousQuantity)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get inventory summary");
                return StatusCode(500, "Internal server error");
            }
        }


        // GET: api/InventoryHistory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryHistory>>> GetInventoryHistories()
        {
            return await _context.InventoryHistories
                .Include(ih => ih.Cycle)
                .Include(ih => ih.Order)
                .OrderByDescending(ih => ih.CreatedAt)
                .ToListAsync();
        }

        // GET: api/InventoryHistory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryHistory>> GetInventoryHistory(int id)
        {
            var inventoryHistory = await _context.InventoryHistories
                .Include(ih => ih.Cycle)
                .Include(ih => ih.Order)
                .FirstOrDefaultAsync(ih => ih.HistoryId == id);

            if (inventoryHistory == null)
            {
                return NotFound();
            }

            return inventoryHistory;
        }

        // GET: api/InventoryHistory/cycle/5
        [HttpGet("cycle/{cycleId}")]
        public async Task<ActionResult<IEnumerable<InventoryHistory>>> GetInventoryHistoryForCycle(int cycleId)
        {
            return await _context.InventoryHistories
                .Where(ih => ih.CycleId == cycleId)
                .Include(ih => ih.Cycle)
                .Include(ih => ih.Order)
                .OrderByDescending(ih => ih.CreatedAt)
                .ToListAsync();
        }

        // POST: api/InventoryHistory
        [HttpPost]
        public async Task<ActionResult<InventoryHistory>> PostInventoryHistory(InventoryHistory inventoryHistory)
        {
            inventoryHistory.CreatedAt = DateTime.UtcNow;
            _context.InventoryHistories.Add(inventoryHistory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetInventoryHistory", new { id = inventoryHistory.HistoryId }, inventoryHistory);
        }
    }
}

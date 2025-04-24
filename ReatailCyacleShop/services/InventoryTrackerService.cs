// Services/InventoryTracker.cs
using RetailCycleShopAPI.Interfaces;
using RetailCycleShopAPI.module;
using Microsoft.Extensions.Logging;

namespace RetailCycleShopAPI.Services
{
    public class InventoryTracker : IInventoryTracker
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryTracker> _logger;

        public InventoryTracker(
            ApplicationDbContext context,
            ILogger<InventoryTracker> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task TrackChangeAsync(
            int cycleId,
            int oldQuantity,
            int newQuantity,
            string reason,
            int? orderId = null)
        {
            try
            {
                var history = new InventoryHistory
                {
                    CycleId = cycleId,
                    OrderId = orderId,
                    PreviousQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    ChangeReason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Tracked inventory change for cycle {CycleId}: {OldQty} → {NewQty} - Reason: {Reason}",
                    cycleId, oldQuantity, newQuantity, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to track inventory change for cycle {CycleId}",
                    cycleId);
                throw;
            }
        }
    }
}
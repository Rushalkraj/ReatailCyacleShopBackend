// Interfaces/IInventoryTracker.cs
using System.Threading.Tasks;

namespace RetailCycleShopAPI.Interfaces
{
    public interface IInventoryTracker
    {
        Task TrackChangeAsync(int cycleId, int oldQuantity, int newQuantity, string reason, int? orderId = null);
    }
}
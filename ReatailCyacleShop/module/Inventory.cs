using System.Text.Json.Serialization;
using RetailCycleShopAPI.module;

namespace RetailCycleShopAPI.module
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int CycleId { get; set; }
        public int StockQuantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public Cycle? Cycle { get; set; }
    }
}

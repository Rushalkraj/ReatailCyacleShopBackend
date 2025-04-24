using System.Text.Json.Serialization;

namespace RetailCycleShopAPI.module
{
    public class Cycle
    {
        public int CycleId { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Inventory? Inventory { get; set; } 
        [JsonIgnore]
        public ICollection<InventoryHistory> InventoryHistories { get; set; } = new List<InventoryHistory>();
    }
};
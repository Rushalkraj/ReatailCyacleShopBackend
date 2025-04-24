using System.Text.Json.Serialization;

namespace RetailCycleShopAPI.module
{
    public class InventoryHistory
    {
        public int HistoryId { get; set; }
        public int CycleId { get; set; }
        public int? OrderId { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string ?ChangeReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public Cycle ?Cycle { get; set; }
        [JsonIgnore]
        public Order ?Order { get; set; }
    }
}
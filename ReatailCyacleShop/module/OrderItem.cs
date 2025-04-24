using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RetailCycleShopAPI.module
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TotalPrice { get; set; }
        public int CycleId { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]

        public Order Order { get; set; } = null!;
        [JsonIgnore]
        public Cycle Cycle { get; set; } = null!;
    }
}
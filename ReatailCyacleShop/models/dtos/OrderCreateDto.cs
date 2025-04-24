using System.Text.Json.Serialization;

namespace RetailCycleShopAPI.models.dtos
{
    public class OrderCreateDto
    {
        public int CustomerId { get; set; }
        public int ShippingAddressId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }

        public List<OrderItemDto> Items { get; set; } = [];
    }
}

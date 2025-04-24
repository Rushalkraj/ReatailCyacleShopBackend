using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.models.dtos
{
    public class PaymentProcessDto
    {
        public int OrderId { get; set; }
        public PaymentType PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public Dictionary<string, string> PaymentDetails { get; set; } = new();
    }
}

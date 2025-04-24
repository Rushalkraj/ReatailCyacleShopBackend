namespace RetailCycleShopAPI.models.dtos
{
    public class PaymentCreateDto
    {
        public required string PaymentMethod { get; set; }
        public required decimal Amount { get; set; }
        public int Status { get; set; }
        public int PaymentType { get; set; }
    }
}

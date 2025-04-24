namespace RetailCycleShopAPI.models.dtos
{
    public class OrderItemDto
    {
        public int CycleId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

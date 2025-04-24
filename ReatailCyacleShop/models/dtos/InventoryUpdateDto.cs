namespace RetailCycleShopAPI.models.dtos
{

    public class InventoryUpdateDto
    {
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
    }
}

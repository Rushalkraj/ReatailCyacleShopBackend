namespace RetailCycleShopAPI.models.dtos
{
    public class CycleCreateDto
    {
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}

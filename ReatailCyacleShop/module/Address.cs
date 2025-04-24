namespace RetailCycleShopAPI.module
{
    public class Address
    {
        public int AddressId { get; set; }
        public required string StreetLine1 { get; set; }
        public string? StreetLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public required string Country { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}
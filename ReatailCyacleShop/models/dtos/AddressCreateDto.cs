namespace RetailCycleShopAPI.models.dtos
{
    public class AddressCreateDto
    {
        public string StreetLine1 { get; set; }
        public string StreetLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }
}

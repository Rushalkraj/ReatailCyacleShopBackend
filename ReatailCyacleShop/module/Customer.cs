using System.Text.Json.Serialization;

namespace RetailCycleShopAPI.module
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public int? BillingAddressId { get; set; }
        public int? ShippingAddressId { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        //[JsonIgnore]
        public Address? BillingAddress { get; set; }
        //[JsonIgnore]
        public Address? ShippingAddress { get; set; }
        [JsonIgnore]
        public List<Order> Orders { get; set; } = new List<Order>();
    }

}
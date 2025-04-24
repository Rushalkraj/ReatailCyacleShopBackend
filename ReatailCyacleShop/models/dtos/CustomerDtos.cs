using System;
using RetailCycleShopAPI.module;

namespace RetailCycleShopAPI.Models.Dtos
{
    public class NewCustomerRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public CustomerAddressDto Address { get; set; }
    }

    public class CustomerAddressDto
    {
        public string StreetLine1 { get; set; }
        public string StreetLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
        public string AddressType { get; set; }
    }

    public class AddressCreateDto
    {
        public string StreetLine1 { get; set; }
        public string StreetLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
    }
}
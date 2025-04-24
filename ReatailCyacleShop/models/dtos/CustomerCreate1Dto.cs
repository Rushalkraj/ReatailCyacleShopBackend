using RetailCycleShopAPI.Models.Dtos;

namespace RetailCycleShopAPI.models.dtos
{
    public class CustomerCreate1Dto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Models.Dtos.AddressCreateDto Address { get; set; }
    }
}

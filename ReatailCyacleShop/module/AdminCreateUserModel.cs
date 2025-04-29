using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.module
{
    public class AdminCreateUserModel
    {
        public string FullName { get; set; } = string.Empty;
        [Key]
        public string Email { get; set; } = string.Empty;
        public string address {  get; set; } = string.Empty;

        public string? phoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;
    }

}

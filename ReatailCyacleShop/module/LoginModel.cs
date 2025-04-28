using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.module
{
    public class LoginModel
    {
        [Key]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

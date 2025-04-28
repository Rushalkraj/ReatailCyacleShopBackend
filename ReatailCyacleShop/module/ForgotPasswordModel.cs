using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.module
{
    public class ForgotPasswordModel
    {
        [Key]
        public string Email { get; set; }
    }
}

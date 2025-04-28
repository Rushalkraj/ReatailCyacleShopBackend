using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.module
{
    public class InvitedUser
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string address { get; set; } = string.Empty;

        //public string? phoneNumber { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime InvitationDate { get; set; } = DateTime.UtcNow;
        public bool IsRegistered { get; set; } = false;
    }
}

namespace RetailCycleShopAPI.models.dtos
{
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}

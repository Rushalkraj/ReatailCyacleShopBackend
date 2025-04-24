namespace RetailCycleShopAPI.module
{

    public class SetupPasswordModel
    {
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ?Email { get; internal set; }
    }
}

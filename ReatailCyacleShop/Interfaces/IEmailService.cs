namespace RetailCycleShopAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordSetupEmail(string email, string name, string link);
    }
}

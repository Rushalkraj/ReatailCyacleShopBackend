namespace RetailCycleShopAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string? email, string v1, string v2);
        Task SendPasswordSetupEmail(string email, string name, string link);
    }
}

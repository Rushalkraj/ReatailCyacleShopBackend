namespace RetailCycleShopAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string htmlContent);
        Task SendPasswordSetupEmail(string email, string name, string link);
    }
}
// Services/EmailService.cs
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RetailCycleShopAPI.Interfaces;

namespace RetailCycleShopAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly HttpClient _httpClient;




        public EmailService(IConfiguration config)
        {
            _apiKey = config["Brevo:ApiKey"] ?? throw new ArgumentNullException("Brevo:ApiKey");
            _senderEmail = config["Brevo:SenderEmail"] ?? throw new ArgumentNullException("Brevo:SenderEmail");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }

        public async Task SendPasswordSetupEmail(string email, string name, string link)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            var emailBody = new
            {
                sender = new { name = "Retail Cycle Shop", email = _senderEmail },
                to = new[] { new { email, name } },
                subject = "Set Your Password",
                htmlContent = $"<p>Hello {name},</p><p><a href='{link}'>Click here to set your password</a></p>"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(emailBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.brevo.com/v3/smtp/email",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Email failed to send: {response.StatusCode} - {responseBody}");
            }
        }
    }
}
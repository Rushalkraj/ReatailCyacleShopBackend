using System.Text;

namespace RetailCycleShopAPI.Services
{
    public class EmailTemplateService
    {
        public string GetPasswordSetupTemplate(string name, string link)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; }");
            sb.AppendLine(".button { background-color: #4a90e2; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }");
            sb.AppendLine(".footer { margin-top: 20px; font-size: 0.8em; color: #666; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<h2>Hello {name},</h2>");
            sb.AppendLine("<p>Welcome to Retail Cycle Shop! Please set your password by clicking the button below:</p>");
            sb.AppendLine($"<p><a href='{link}' class='button'>Set Password</a></p>");
            sb.AppendLine($"<p>Or copy and paste this link into your browser:<br>{link}</p>");
            sb.AppendLine("<p>This link will expire in 24 hours.</p>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>If you didn't request this, please ignore this email.</p>");
            sb.AppendLine("<p>Thanks,<br>The Retail Cycle Shop Team</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
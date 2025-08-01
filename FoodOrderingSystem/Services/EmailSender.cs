using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FoodOrderingSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        // Add a logger to see the response from SendGrid
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid API Key or From Email is not configured in user secrets.");
                return;
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email to {Email} sent successfully!", email);
            }
            else
            {
                _logger.LogError("Failed to send email. Status Code: {StatusCode}", response.StatusCode);
                // Log the full response body to see the exact error from SendGrid
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid Response: {ResponseBody}", responseBody);
            }
        }
    }
}

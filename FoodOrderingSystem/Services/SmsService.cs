using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodOrderingSystem.Services
{
    public class SmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IConfiguration _configuration;

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            // Placeholder implementation - in production, integrate with SMS service
            _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", phoneNumber, message);
            await Task.CompletedTask;
        }

        public async Task SendOrderConfirmationAsync(string phoneNumber, string orderNumber, decimal total)
        {
            var message = $"Your order #{orderNumber} has been confirmed! Total: ${total}. Thank you for choosing us!";
            await SendSmsAsync(phoneNumber, message);
        }

        public async Task SendOrderStatusUpdateAsync(string phoneNumber, string orderNumber, string status)
        {
            var message = $"Your order #{orderNumber} status has been updated to: {status}";
            await SendSmsAsync(phoneNumber, message);
        }

        public async Task SendPromotionalMessageAsync(string phoneNumber, string promotion)
        {
            var message = $"Special offer: {promotion}. Visit us today!";
            await SendSmsAsync(phoneNumber, message);
        }

        public async Task SendTwoFactorCodeAsync(string phoneNumber, string code)
        {
            var message = $"Your MackDihh verification code is: {code}. This code will expire in 10 minutes.";
            await SendSmsAsync(phoneNumber, message);
        }
    }
}

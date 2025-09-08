using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodOrderingSystem.Services
{
    public class CustomSmsTokenProvider<TUser> : PhoneNumberTokenProvider<TUser> where TUser : class
    {
        private readonly SmsService _smsService;
        private readonly ILogger<CustomSmsTokenProvider<TUser>> _logger;

        public CustomSmsTokenProvider(SmsService smsService, ILogger<CustomSmsTokenProvider<TUser>> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var token = await base.GenerateAsync(purpose, manager, user);
            
            // Get user's phone number
            var phoneNumber = await manager.GetPhoneNumberAsync(user);
            
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                // Send SMS with the token
                await _smsService.SendTwoFactorCodeAsync(phoneNumber, token);
                _logger.LogInformation("2FA SMS sent to {PhoneNumber}", phoneNumber);
            }
            else
            {
                _logger.LogWarning("Cannot send 2FA SMS - no phone number found for user");
            }
            
            return token;
        }
    }
}

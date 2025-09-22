using Microsoft.Extensions.Configuration;

namespace FoodOrderingSystem.Services
{
    public class TimeZoneService
    {
        private readonly IConfiguration _configuration;
        private readonly TimeZoneInfo _localTimeZone;

        public TimeZoneService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Get the configured time zone or default to Malaysia Standard Time
            var timeZoneId = _configuration["TimeZoneSettings:DefaultTimeZone"] ?? "Malaysia Standard Time";
            
            try
            {
                _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                // Fallback to UTC if the time zone is not found
                _localTimeZone = TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// Gets the current local time in the configured time zone
        /// </summary>
        public DateTime GetLocalTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _localTimeZone);
        }

        /// <summary>
        /// Converts a UTC time to local time
        /// </summary>
        public DateTime ConvertFromUtc(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _localTimeZone);
        }

        /// <summary>
        /// Converts a local time to UTC
        /// </summary>
        public DateTime ConvertToUtc(DateTime localTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localTime, _localTimeZone);
        }

        /// <summary>
        /// Gets the current local time plus specified minutes
        /// </summary>
        public DateTime GetLocalTimePlusMinutes(int minutes)
        {
            return GetLocalTime().AddMinutes(minutes);
        }
    }
}

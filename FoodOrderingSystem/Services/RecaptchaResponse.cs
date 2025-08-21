using System.Text.Json.Serialization;

namespace FoodOrderingSystem.Services
{
    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public required string Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public required string Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public required List<string> ErrorCodes { get; set; }
    }
}
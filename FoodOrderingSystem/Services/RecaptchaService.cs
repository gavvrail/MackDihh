using System.Text.Json;

namespace FoodOrderingSystem.Services
{
    public class RecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["RecaptchaSettings:SecretKey"];
        }

        public virtual async Task<bool> Validate(string token)
        {
            // If the token is null or empty, it's invalid.
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            // This creates the request to send to Google's server
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"secret", _secretKey},
                {"response", token}
            });

            try
            {
                // Send the request and get the response
                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response from Google
                var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(responseString);

                // Return true if Google says success is true, otherwise false.
                return recaptchaResponse?.Success ?? false;
            }
            catch (HttpRequestException ex)
            {
                // Handle exceptions if your server can't reach Google's server
                // For now, we can log this and return false
                Console.WriteLine($"Error verifying reCAPTCHA: {ex.Message}");
                return false;
            }
        }
    }
}
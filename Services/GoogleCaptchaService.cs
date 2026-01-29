using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRMS.Services
{
    public class GoogleCaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GoogleCaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyToken(string token)
        {
            try
            {
                var secretKey = _configuration["Recaptcha:SecretKey"];
                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}";

                var response = await _httpClient.GetAsync(url);
                var jsonString = await response.Content.ReadAsStringAsync();

                // DEBUGGING: Print Google's response to the Console
                Console.WriteLine($"[RECAPTCHA DEBUG] Response: {jsonString}");

                if (response.IsSuccessStatusCode)
                {
                    var captchaResponse = JsonSerializer.Deserialize<CaptchaResponse>(jsonString);

                    // If Success is false, check the console for "error-codes"
                    if (!captchaResponse.Success) return false;

                    // On Localhost, we lower the threshold to 0.1 to prevent blocking
                    // Adjust this to 0.5 when you go to Production
                    return captchaResponse.Score >= 0.1;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECAPTCHA ERROR] {ex.Message}");
                return false;
            }
        }

        private class CaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }

            [JsonPropertyName("action")]
            public string Action { get; set; }

            [JsonPropertyName("error-codes")]
            public List<string> ErrorCodes { get; set; }
        }
    }
}
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

                // 1. Log response
                Console.WriteLine($"[RECAPTCHA DEBUG] Response: {jsonString}");

                if (response.IsSuccessStatusCode)
                {
                    // 2. Deserialize safely
                    var captchaResponse = JsonSerializer.Deserialize<CaptchaResponse>(jsonString);

                    // 3. Null check 'captchaResponse' (Fixes CS8602)
                    if (captchaResponse == null) return false;

                    if (!captchaResponse.Success) return false;

                    // 4. Adjust score threshold if needed
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

        // Fixes CS8618: Made properties nullable (?) so compiler knows they might be empty
        private class CaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }

            [JsonPropertyName("action")]
            public string? Action { get; set; } // Added '?'

            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; } // Added '?'
        }
    }
}
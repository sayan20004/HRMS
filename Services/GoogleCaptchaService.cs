using System.Text.Json;

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
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var captchaResponse = JsonSerializer.Deserialize<CaptchaResponse>(jsonString);
                    return captchaResponse?.Success ?? false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private class CaptchaResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
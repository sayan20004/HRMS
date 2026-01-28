using System.ComponentModel.DataAnnotations;

namespace HRMS.Models
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Otp { get; set; } = string.Empty;
    }
}
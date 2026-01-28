using System.ComponentModel.DataAnnotations;

namespace HRMS.Models
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
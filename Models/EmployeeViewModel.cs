using System.ComponentModel.DataAnnotations;

namespace HRMS.Models
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Designation { get; set; } = string.Empty;
    }
}
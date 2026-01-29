using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRMS.Models
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        // --- INPUT FIELDS (For Forms) ---
        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public int DesignationId { get; set; }

        // --- DISPLAY OBJECTS (For Lists) ---
        // These match the nested JSON structure from the API
        public DepartmentViewModel? Department { get; set; }
        public DesignationViewModel? Designation { get; set; }
    }

    public class DepartmentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class DesignationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } 
    }
}
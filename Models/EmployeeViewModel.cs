namespace HRMS.Models
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }

        // Relationships
        public int? DepartmentId { get; set; }
        public DepartmentViewModel? Department { get; set; }

        public int? DesignationId { get; set; }
        public DesignationViewModel? Designation { get; set; }

        // Added Post
        public int? PostId { get; set; }
        public Post? Post { get; set; }
    }
}
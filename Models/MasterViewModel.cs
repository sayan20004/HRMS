using System.ComponentModel.DataAnnotations;

namespace HRMS.Models
{
    public class MasterViewModel
    {
        // --- INPUT FIELDS (For the Card) ---
        [Display(Name = "Department Name")]
        public string? DeptName { get; set; }

        [Display(Name = "Dept Code (e.g. IT)")]
        public string? DeptCode { get; set; }

        [Display(Name = "Designation Name")]
        public string? DesigName { get; set; }

        [Display(Name = "Level (1-10)")]
        public int? DesigLevel { get; set; }

        // --- DISPLAY LISTS (For the Tables) ---
        public List<DepartmentViewModel> Departments { get; set; } = new List<DepartmentViewModel>();
        public List<DesignationViewModel> Designations { get; set; } = new List<DesignationViewModel>();
    }
}
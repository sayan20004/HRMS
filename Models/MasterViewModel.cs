using System.Collections.Generic;

namespace HRMS.Models
{
    public class MasterViewModel
    {
        // --- Form Inputs ---
        public string? DeptName { get; set; }
        public string? DeptCode { get; set; }
        public string? DesigName { get; set; }
        public int? DesigLevel { get; set; }
        
        // --- Lists for Display Tables ---
        public List<DepartmentViewModel> Departments { get; set; } = new List<DepartmentViewModel>();
        public List<DesignationViewModel> Designations { get; set; } = new List<DesignationViewModel>();
        
        // Added List for Posts
        public List<Post> Posts { get; set; } = new List<Post>();
    }
}
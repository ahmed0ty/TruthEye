using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100, ErrorMessage = "Course name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course description is required")]
        [StringLength(500, ErrorMessage = "Course description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department ID is required")]
        public int DepartmentId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class DoctorCourseDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100, ErrorMessage = "Course name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course description is required")]
        [StringLength(500, ErrorMessage = "Course description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public int StudentCount { get; set; }
        public int ExamCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> InstructorNames { get; set; } = new List<string>();

    }
}

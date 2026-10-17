using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class UpdateFacultyDto
    {
        [Required(ErrorMessage = "Faculty name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "University ID is required")]
        public int UniversityId { get; set; }
    }
}

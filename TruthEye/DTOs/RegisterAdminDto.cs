using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class RegisterAdminDto:RegisterUserDto
    {
        [Required(ErrorMessage = "University ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid university ID")]
        public int UniversityId { get; set; }

        [Required(ErrorMessage = "Faculty ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid faculty ID")]
        public int FacultyId { get; set; }
    }
}

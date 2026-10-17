using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class UpdateAdminDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "University ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid university ID")]
        public int UniversityId { get; set; }

        [Required(ErrorMessage = "Faculty ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid faculty ID")]
        public int FacultyId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class RegisterProctorDto : RegisterUserDto
    {
        [Required(ErrorMessage = "Department is required")]
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; } = string.Empty;
    }

}

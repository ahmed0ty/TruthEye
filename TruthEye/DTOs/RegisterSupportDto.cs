using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class RegisterSupportDto : RegisterUserDto
    {
        [Required(ErrorMessage = "Department is required")]
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shift timing is required")]
        [StringLength(50, ErrorMessage = "Shift timing cannot exceed 50 characters")]
        public string ShiftTiming { get; set; } = string.Empty;
    }
}

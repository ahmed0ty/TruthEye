using System.ComponentModel.DataAnnotations;
using TruthEye.Data.Enum;

namespace TruthEye.DTOs
{
    public class UpdateStudentDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

   
        [Required(ErrorMessage = "Department ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid department ID")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [EnumDataType(typeof(Year), ErrorMessage = "Invalid year value")]
        public Year Year { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

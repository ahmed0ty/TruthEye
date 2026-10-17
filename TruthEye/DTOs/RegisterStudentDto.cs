using System.ComponentModel.DataAnnotations;
using TruthEye.Data.Enum;

namespace TruthEye.DTOs
{
    public class RegisterStudentDto: RegisterUserDto
    {
        
        [Required(ErrorMessage = "Department ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid department ID")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [EnumDataType(typeof(Year), ErrorMessage = "Invalid year value")]
        public Year Year { get; set; }
    }
}

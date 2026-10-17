using System.ComponentModel.DataAnnotations;
using TruthEye.Data.Enum;

namespace TruthEye.DTOs
{
    public class RegisterDoctorDto : RegisterUserDto
    {
      

        [Required(ErrorMessage = "Department ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid department ID")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic degree is required")]
        [EnumDataType(typeof(AcademicDegree), ErrorMessage = "Invalid academic degree")]
        public AcademicDegree AcademicDegree { get; set; }

        [Required(ErrorMessage = "Years of experience is required")]
        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
        public int YearsOfExperience { get; set; }
    }
}

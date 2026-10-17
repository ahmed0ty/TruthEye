using TruthEye.Data.Enum;

namespace TruthEye.DTOs
{
     /// <summary>
    /// Data transfer object for Doctor registration and responses
    /// </summary>
    public class DoctorDto:UserDto
    {
        public int UniversityId { get; set; }
        public string UniversityName { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public AcademicDegree AcademicDegree { get; set; }
        public int YearsOfExperience { get; set; }
    }
}

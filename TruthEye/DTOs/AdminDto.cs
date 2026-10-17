using TruthEye.Entities;

namespace TruthEye.DTOs
{
    
    public class AdminDto : UserDto
    {
        public int UniversityId { get; set; }
        public string UniversityName { get; set; } = string.Empty; 
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty; 
    }
}

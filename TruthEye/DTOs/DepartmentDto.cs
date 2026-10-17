namespace TruthEye.DTOs
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public int CoursesCount { get; set; }
    }
}

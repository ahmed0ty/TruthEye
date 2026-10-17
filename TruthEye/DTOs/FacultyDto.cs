namespace TruthEye.DTOs
{
    public class FacultyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UniversityId { get; set; }
        public string UniversityName { get; set; } = string.Empty;
        public int DepartmentsCount { get; set; }
    }
}

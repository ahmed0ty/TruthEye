using TruthEye.Data.Enum;

namespace TruthEye.Entities
{
    public class Student:User
    {
        public int UniversityId { get; set; }
        public virtual University University { get; set; }

        public int FacultyId { get; set; }
        public virtual Faculty Faculty { get; set; }
        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; }
        public  Year Year { get; set; }
        public virtual ICollection<Course> RegisteredCourses { get; set; }
        public virtual ICollection<ExamSession> ExamSessions { get; set; }

        public virtual ICollection<PerformanceReport> PerformanceReports { get; set; }
    }
}

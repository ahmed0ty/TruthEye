using TruthEye.Data.Enum;

namespace TruthEye.Entities
{
    public class Doctor : User
    {
        public int UniversityId { get; set; }
        public virtual University University { get; set; }

        public int FacultyId { get; set; }
        public virtual Faculty Faculty { get; set; }

        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string Specialization { get; set; } = string.Empty;
        public AcademicDegree AcademicDegree { get; set; }
        public int YearsOfExperience { get; set; }
        public virtual ICollection<Exam> CreatedExams { get; set; }
        public virtual ICollection<Course> TaughtCourses { get; set; }

    }
}

namespace TruthEye.Entities
{
    public class StudentCourse
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; }

        public Course Course { get; set; }
    }
}

namespace TruthEye.Entities
{
    public class Proctor : User
    {

        public string Department { get; set; }
        public bool IsOnDuty { get; set; }
        public int ActiveSessionsCount { get; set; }
        public DateTime? LastDutyDate { get; set; }
        public double AverageResponseTime { get; set; }
        public virtual ICollection<Exam> AssignedExams { get; set; }
        public virtual ICollection<AILog> ReviewedAlerts { get; set; }
        public virtual ICollection<ProctorSession> ProctorSessions { get; set; }
    }
}

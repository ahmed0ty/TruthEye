namespace TruthEye.Entities
{
    public class AILog
    {
        public int Id { get; set; }
        public string EventType { get; set; } 
        public DateTime Timestamp { get; set; }
        public string ScreenshotPath { get; set; }
        public double Confidence { get; set; }
        public int ExamSessionId { get; set; }
        public virtual ExamSession ExamSession { get; set; }
    }
}

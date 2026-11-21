namespace TruthEye.DTOs
{
    public class BucketDto { public string Label { get; set; } = ""; public int Count { get; set; } }

    public class QuestionStatDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = "";
        public string Type { get; set; } = "";
        public double MaxMarks { get; set; }
        public int Answered { get; set; }
        public int Skipped { get; set; }
        public double AvgMarks { get; set; }
        public double ScoreRate { get; set; }      // 0..100
        public string Difficulty { get; set; } = "Medium";
    }

    public class ExamAnalyticsDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = "";
        public int Students { get; set; }
        public double AvgPercentage { get; set; }
        public double PassRate { get; set; }
        public double Highest { get; set; }
        public double Lowest { get; set; }
        public int Flagged { get; set; }
        public List<BucketDto> Buckets { get; set; } = new();
        public List<QuestionStatDto> Questions { get; set; } = new();
    }

    public class CourseStatDto
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = "";
        public int Exams { get; set; }
        public int Sessions { get; set; }
        public double AvgPercentage { get; set; }
        public double PassRate { get; set; }
        public int Flagged { get; set; }
    }

    public class HourStatDto { public int Hour { get; set; } public int Count { get; set; } }
    public class DayStatDto { public string Date { get; set; } = ""; public int Sessions { get; set; } public int Flagged { get; set; } }

    public class FacultyAnalyticsDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalSessions { get; set; }
        public double AvgPercentage { get; set; }
        public double PassRate { get; set; }
        public int FlaggedSessions { get; set; }
        public double FlaggedRate { get; set; }
        public List<CourseStatDto> Courses { get; set; } = new();
        public List<HourStatDto> Hours { get; set; } = new();
        public List<DayStatDto> Daily { get; set; } = new();
    }
}
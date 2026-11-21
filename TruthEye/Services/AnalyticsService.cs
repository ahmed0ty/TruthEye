using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Exceptions;
using TruthEye.Security;

namespace TruthEye.Services
{
    public interface IAnalyticsService
    {
        Task<ExamAnalyticsDto> ExamAsync(int doctorId, int examId);
        Task<FacultyAnalyticsDto> FacultyAsync(int actorId, DateTime? from, DateTime? to);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private const int FlaggedAt = 10;
        private readonly AppDbContext _db;

        public AnalyticsService(AppDbContext db) => _db = db;

        public static List<BucketDto> Bucketize(IEnumerable<double> percentages)
        {
            var counts = new int[10];
            foreach (var p in percentages)
            {
                var i = (int)Math.Floor(Math.Clamp(p, 0, 100) / 10);
                counts[Math.Min(i, 9)]++;     // 100% تدخل في آخر شريحة
            }
            return Enumerable.Range(0, 10).Select(i => new BucketDto
            {
                Label = i == 9 ? "90-100" : $"{i * 10}-{i * 10 + 9}",
                Count = counts[i]
            }).ToList();
        }

        private static double Pct(int part, int whole) => whole == 0 ? 0 : Math.Round(part * 100.0 / whole, 1);

        public async Task<ExamAnalyticsDto> ExamAsync(int doctorId, int examId)
        {
            var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);
            if (doctor == null || !doctor.IsActive) throw new ForbiddenException("Your account is disabled");

            var exam = await _db.Exams.AsNoTracking()
                .Where(e => e.Id == examId && !e.IsDeleted && e.DoctorId == doctorId)
                .Select(e => new { e.Id, e.Title })
                .FirstOrDefaultAsync() ?? throw new NotFoundException("Exam not found");

            var sessions = await _db.ExamSessions.AsNoTracking()
                .Where(s => s.ExamId == examId && s.Status != "InProgress")
                .Select(s => new { s.Id, s.Percentage, s.IsPassed })
                .ToListAsync();

            var logs = await _db.AILogs.AsNoTracking()
                .Where(l => l.ExamSession.ExamId == examId && l.ExamSession.Status != "InProgress")
                .GroupBy(l => new { l.ExamSessionId, l.EventType })
                .Select(g => new { g.Key.ExamSessionId, g.Key.EventType, Count = g.Count() })
                .ToListAsync();
            var flagged = logs.GroupBy(l => l.ExamSessionId)
                .Count(g => RiskCalculator.Score(g.Select(x => (x.EventType, x.Count))) >= FlaggedAt);

            var answers = await _db.Answers.AsNoTracking()
                .Where(a => a.ExamSession.ExamId == examId && a.ExamSession.Status != "InProgress")
                .GroupBy(a => a.QuestionId)
                .Select(g => new { QuestionId = g.Key, Count = g.Count(), Sum = g.Sum(x => x.MarksObtained) })
                .ToDictionaryAsync(x => x.QuestionId);

            var questions = await _db.Questions.AsNoTracking()
                .Where(q => q.ExamId == examId)
                .OrderBy(q => q.Id)
                .Select(q => new { q.Id, q.QuestionText, q.QuestionType, q.Marks })
                .ToListAsync();

            var n = sessions.Count;

            return new ExamAnalyticsDto
            {
                ExamId = exam.Id,
                Title = exam.Title,
                Students = n,
                AvgPercentage = n == 0 ? 0 : Math.Round(sessions.Average(s => s.Percentage), 1),
                PassRate = Pct(sessions.Count(s => s.IsPassed), n),
                Highest = n == 0 ? 0 : Math.Round(sessions.Max(s => s.Percentage), 1),
                Lowest = n == 0 ? 0 : Math.Round(sessions.Min(s => s.Percentage), 1),
                Flagged = flagged,
                Buckets = Bucketize(sessions.Select(s => s.Percentage)),
                Questions = questions.Select(q =>
                {
                    answers.TryGetValue(q.Id, out var a);
                    var answered = a?.Count ?? 0;
                    var sum = a?.Sum ?? 0;
                    var rate = n == 0 || q.Marks <= 0 ? 0 : Math.Round(sum / (n * (double)q.Marks) * 100, 1);
                    return new QuestionStatDto
                    {
                        QuestionId = q.Id,
                        Text = q.QuestionText,
                        Type = q.QuestionType,
                        MaxMarks = q.Marks,
                        Answered = answered,
                        Skipped = Math.Max(0, n - answered),
                        AvgMarks = answered == 0 ? 0 : Math.Round(sum / answered, 2),
                        ScoreRate = rate,
                        Difficulty = rate < 40 ? "Hard" : rate < 70 ? "Medium" : "Easy"
                    };
                }).OrderBy(q => q.ScoreRate).ToList()    // الأصعب الأول
            };
        }

        public async Task<FacultyAnalyticsDto> FacultyAsync(int actorId, DateTime? fromD, DateTime? toD)
        {
            var actor = await _db.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actorId && !a.IsDeleted);
            if (actor == null || !actor.IsActive) throw new ForbiddenException("Your account is disabled");
            var fid = actor.FacultyId;

            var to = (toD ?? DateTime.UtcNow).Date.AddDays(1);
            var from = (fromD ?? to.AddDays(-30)).Date;
            if (from >= to) throw new BadRequestException("Invalid date range");
            if ((to - from).TotalDays > 366) from = to.AddDays(-366);

            var sessions = await _db.ExamSessions.AsNoTracking()
                .Where(s => s.Status != "InProgress" && !s.Exam.IsDeleted
                            && s.Student.FacultyId == fid && s.StartTime >= from && s.StartTime < to)
                .Select(s => new
                {
                    s.Id, s.ExamId, s.Exam.CourseId, CourseName = s.Exam.Course.Name,
                    s.Percentage, s.IsPassed, s.StartTime
                })
                .ToListAsync();

            var logs = await _db.AILogs.AsNoTracking()
                .Where(l => l.ExamSession.Status != "InProgress" && !l.ExamSession.Exam.IsDeleted
                            && l.ExamSession.Student.FacultyId == fid
                            && l.ExamSession.StartTime >= from && l.ExamSession.StartTime < to)
                .GroupBy(l => new { l.ExamSessionId, l.EventType })
                .Select(g => new { g.Key.ExamSessionId, g.Key.EventType, Count = g.Count() })
                .ToListAsync();

            var flaggedIds = logs.GroupBy(l => l.ExamSessionId)
                .Where(g => RiskCalculator.Score(g.Select(x => (x.EventType, x.Count))) >= FlaggedAt)
                .Select(g => g.Key).ToHashSet();

            var total = sessions.Count;

            return new FacultyAnalyticsDto
            {
                From = from,
                To = to.AddDays(-1),
                TotalSessions = total,
                AvgPercentage = total == 0 ? 0 : Math.Round(sessions.Average(s => s.Percentage), 1),
                PassRate = Pct(sessions.Count(s => s.IsPassed), total),
                FlaggedSessions = flaggedIds.Count,
                FlaggedRate = Pct(flaggedIds.Count, total),
                Courses = sessions.GroupBy(s => new { s.CourseId, s.CourseName })
                    .Select(g => new CourseStatDto
                    {
                        CourseId = g.Key.CourseId,
                        Name = g.Key.CourseName,
                        Exams = g.Select(x => x.ExamId).Distinct().Count(),
                        Sessions = g.Count(),
                        AvgPercentage = Math.Round(g.Average(x => x.Percentage), 1),
                        PassRate = Pct(g.Count(x => x.IsPassed), g.Count()),
                        Flagged = g.Count(x => flaggedIds.Contains(x.Id))
                    }).OrderByDescending(c => c.Flagged).ThenByDescending(c => c.Sessions).ToList(),
                Hours = Enumerable.Range(0, 24)
                    .Select(h => new HourStatDto { Hour = h, Count = sessions.Count(s => s.StartTime.Hour == h) }).ToList(),
                Daily = sessions.GroupBy(s => s.StartTime.Date).OrderBy(g => g.Key)
                    .Select(g => new DayStatDto
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Sessions = g.Count(),
                        Flagged = g.Count(x => flaggedIds.Contains(x.Id))
                    }).ToList()
            };
        }
    }
}
using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Exceptions;
using TruthEye.Security;

namespace TruthEye.Services
{
    public interface IGradingService
    {
        Task<GradingSessionDto> GetSessionAsync(int doctorId, int sessionId);
        Task<GradingSessionDto> AdjustAsync(int doctorId, int answerId, AdjustGradeDto dto);
    }

    public class GradingService : IGradingService
    {
        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;
        private readonly INotificationService _notifications;
        private readonly ILogger<GradingService> _logger;

        public GradingService(AppDbContext db, ISystemLogService audit, INotificationService notifications, ILogger<GradingService> logger)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _logger = logger;
        }

        private async Task<Doctor> DoctorAsync(int id)
        {
            var d = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (d == null || !d.IsActive) throw new ForbiddenException("Your account is disabled");
            return d;
        }

        public async Task<GradingSessionDto> GetSessionAsync(int doctorId, int sessionId)
        {
            await DoctorAsync(doctorId);

            var s = await _db.ExamSessions.AsNoTracking()
                .Where(x => x.Id == sessionId && x.Status != "InProgress"
                            && !x.Exam.IsDeleted && x.Exam.DoctorId == doctorId)
                .Select(x => new
                {
                    x.Id,
                    Student = x.Student.FirstName + " " + x.Student.LastName,
                    Exam = x.Exam.Title,
                    x.TotalScore, x.TotalMarks, x.Percentage, x.IsPassed,
                    Passing = x.Exam.PassingScore
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundException("Session not found");

            var answers = await _db.Answers.AsNoTracking()
                .Include(a => a.Question)
                .Include(a => a.SelectedOption)
                .Where(a => a.ExamSessionId == sessionId)
                .OrderBy(a => a.QuestionId)
                .ToListAsync();

            var adjustments = await _db.GradeAdjustments.AsNoTracking()
                .Where(g => g.ExamSessionId == sessionId)
                .OrderByDescending(g => g.ChangedAt)
                .ToListAsync();

            return new GradingSessionDto
            {
                SessionId = s.Id,
                StudentName = s.Student,
                ExamTitle = s.Exam,
                TotalScore = s.TotalScore,
                TotalMarks = s.TotalMarks,
                Percentage = s.Percentage,
                IsPassed = s.IsPassed,
                PassingScore = s.Passing,
                Answers = answers.Select(a =>
                {
                    var last = adjustments.FirstOrDefault(g => g.AnswerId == a.Id);
                    return new GradingAnswerDto
                    {
                        AnswerId = a.Id,
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.QuestionText,
                        QuestionType = a.Question.QuestionType,
                        MaxMarks = a.Question.Marks,
                        MarksObtained = a.MarksObtained,
                        StudentAnswer = a.AnswerText,
                        SelectedOption = a.SelectedOption?.OptionText,
                        ModelAnswer = (a.Question as WrittenQuestion)?.ModelAnswer,
                        AiSimilarity = a.AISimilarityScore,
                        AiFeedback = a.AIFeedback,
                        Adjusted = last != null,
                        LastReason = last?.Reason
                    };
                }).ToList()
            };
        }

        public async Task<GradingSessionDto> AdjustAsync(int doctorId, int answerId, AdjustGradeDto dto)
        {
            var doctor = await DoctorAsync(doctorId);

            var reason = dto.Reason?.Trim() ?? "";
            if (reason.Length < 3) throw new BadRequestException("Add a reason for the change");
            if (reason.Length > 500) throw new BadRequestException("The reason is too long");
            if (double.IsNaN(dto.Marks) || double.IsInfinity(dto.Marks))
                throw new BadRequestException("Invalid marks");

            var answer = await _db.Answers
                .Include(a => a.Question)
                .Include(a => a.ExamSession).ThenInclude(s => s.Exam)
                .Include(a => a.ExamSession).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == answerId)
                ?? throw new NotFoundException("Answer not found");

            var session = answer.ExamSession;
            if (session.Exam.IsDeleted || session.Exam.DoctorId != doctorId)
                throw new NotFoundException("Answer not found");
            if (session.Status == "InProgress")
                throw new BadRequestException("The exam session is still in progress");

            var marks = Math.Round(dto.Marks, 2);
            if (marks < 0 || marks > answer.Question.Marks)
                throw new BadRequestException($"Marks must be between 0 and {answer.Question.Marks}");
            if (Math.Abs(marks - answer.MarksObtained) < 0.001)
                throw new BadRequestException("The marks are already the same");

            var old = answer.MarksObtained;
            var oldTotal = session.TotalScore;

            answer.MarksObtained = marks;
            answer.IsCorrect = marks >= answer.Question.Marks;

            var others = await _db.Answers
                .Where(a => a.ExamSessionId == session.Id && a.Id != answerId)
                .SumAsync(a => a.MarksObtained);

            session.TotalScore = others + marks;
            session.Percentage = session.TotalMarks > 0 ? session.TotalScore / session.TotalMarks * 100 : 0;
            session.IsPassed = session.Percentage >= session.Exam.PassingScore;

            _db.GradeAdjustments.Add(new GradeAdjustment
            {
                AnswerId = answerId,
                ExamSessionId = session.Id,
                OldMarks = old,
                NewMarks = marks,
                Reason = reason,
                ChangedBy = doctor.Id,
                ChangedAt = DateTime.UtcNow
            });

            _audit.Stage(new AuditEntry
            {
                ActorId = doctor.Id,
                ActorRole = AppRoles.Doctor,
                FacultyId = session.Student.FacultyId,
                Action = ActivityActions.GradeAdjusted,
                EntityType = ActivityEntities.Grade,
                EntityId = answerId,
                Description = $"Changed a grade of {session.Student.FirstName} {session.Student.LastName} in \"{session.Exam.Title}\": {old} → {marks}",
                OldValues = new { Marks = old, Total = oldTotal },
                NewValues = new { Marks = marks, Total = session.TotalScore, Reason = reason }
            });

            await _db.SaveChangesAsync();

            try
            {
                await _notifications.SendNotificationAsync(session.StudentId,
                    $"Your grade in \"{session.Exam.Title}\" was updated", "Result");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Grade notification failed for session {SessionId}", session.Id); }

            return await GetSessionAsync(doctorId, session.Id);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Exceptions;
using TruthEye.Security;

namespace TruthEye.Services
{
    public interface IResumeService
    {
        Task<ResumeStatusDto> StatusAsync(int studentId, int sessionId);
        Task<ResumeStatusDto> RequestAsync(int studentId, CreateResumeDto dto);
        Task<List<ResumeRowDto>> PendingAsync(int actorId);
        Task<ResumeRowDto> DecideAsync(int actorId, int requestId, ResumeDecisionDto dto);
    }

    public class ResumeService : IResumeService
    {
        private static readonly TimeSpan AskWithin = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DecideWithin = TimeSpan.FromMinutes(15);

        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;
        private readonly INotificationService _notifications;
        private readonly ILogger<ResumeService> _logger;

        public ResumeService(AppDbContext db, ISystemLogService audit, INotificationService notifications, ILogger<ResumeService> logger)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _logger = logger;
        }

        private static DateTime Deadline(ExamSession s, Exam e)
        {
            var byDuration = s.StartTime.AddMinutes(e.Duration);
            return byDuration < e.EndTime ? byDuration : e.EndTime;
        }

        /// ليه مينفعش الاستئناف؟ null = ينفع
        private static string? WhyNot(ExamSession s, Exam e, bool alreadyAsked, DateTime now)
        {
            if (s.Status != "Terminated" || s.EndReason != "lost-contact") return "This exam can't be resumed";
            if (alreadyAsked) return "A resume was already requested for this exam";
            if (s.EndTime == null || now - s.EndTime.Value > AskWithin) return "The request window has passed";
            if (now >= Deadline(s, e)) return "The exam time is over";
            return null;
        }

        public async Task<ResumeStatusDto> StatusAsync(int studentId, int sessionId)
        {
            var s = await _db.ExamSessions.AsNoTracking().Include(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.StudentId == studentId)
                ?? throw new NotFoundException("Session not found");

            var req = await _db.ResumeRequests.AsNoTracking().FirstOrDefaultAsync(r => r.ExamSessionId == sessionId);
            var why = WhyNot(s, s.Exam, req != null, DateTime.UtcNow);

            return new ResumeStatusDto { CanRequest = why == null, Status = req?.Status, Note = req?.Note, Why = why };
        }

        public async Task<ResumeStatusDto> RequestAsync(int studentId, CreateResumeDto dto)
        {
            var reason = dto.Reason?.Trim() ?? "";
            if (reason.Length < 5 || reason.Length > 500) throw new BadRequestException("Explain what happened (5 to 500 characters)");

            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentId && !x.IsDeleted);
            if (student == null || !student.IsActive) throw new ForbiddenException("Your account is disabled");

            var s = await _db.ExamSessions.Include(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == dto.ExamSessionId && x.StudentId == studentId)
                ?? throw new NotFoundException("Session not found");

            var asked = await _db.ResumeRequests.AnyAsync(r => r.ExamSessionId == s.Id);
            var why = WhyNot(s, s.Exam, asked, DateTime.UtcNow);
            if (why != null) throw new BadRequestException(why);

            _db.ResumeRequests.Add(new ResumeRequest
            {
                ExamSessionId = s.Id, StudentId = studentId, Reason = reason, Status = "Pending", CreatedAt = DateTime.UtcNow
            });

            _audit.Stage(new AuditEntry
            {
                ActorId = studentId, ActorRole = AppRoles.Student, FacultyId = student.FacultyId,
                Action = ActivityActions.ResumeRequested, EntityType = ActivityEntities.Resume, EntityId = s.Id,
                Description = $"{student.FirstName} {student.LastName} asked to resume \"{s.Exam.Title}\" after a disconnection"
            });

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new ConflictException("A resume was already requested for this exam"); }

            try
            {
                await _notifications.SendNotificationAsync(s.Exam.DoctorId,
                    $"{student.FirstName} {student.LastName} asked to resume \"{s.Exam.Title}\"", "Resume");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Resume notification failed"); }

            return await StatusAsync(studentId, s.Id);
        }

        // الدكتور: طلبات امتحاناته. المراقب: طلاب كليته.
        private async Task<(User Actor, string Role, IQueryable<ResumeRequest> Scope)> ScopeAsync(int actorId)
        {
            var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId && !u.IsDeleted);
            if (actor == null || !actor.IsActive) throw new ForbiddenException("Your account is disabled");

            var role = StaffRules.RoleOf(actor);
            var all = _db.ResumeRequests.AsQueryable();

            return role switch
            {
                AppRoles.Doctor => (actor, role, all.Where(r => _db.ExamSessions.Any(s => s.Id == r.ExamSessionId && s.Exam.DoctorId == actorId))),
                AppRoles.Proctor => (actor, role, all.Where(r => _db.ExamSessions.Any(s => s.Id == r.ExamSessionId && s.Student.FacultyId == ChatRules.FacultyOf(actor)))),
                _ => throw new ForbiddenException()
            };
        }

        public async Task<List<ResumeRowDto>> PendingAsync(int actorId)
        {
            var (_, _, scope) = await ScopeAsync(actorId);
            var now = DateTime.UtcNow;

            var rows = await scope.AsNoTracking()
                .Where(r => r.Status == "Pending" && r.CreatedAt > now - DecideWithin)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id, r.ExamSessionId, r.Reason, r.Status, r.Note, r.CreatedAt,
                    Student = _db.ExamSessions.Where(s => s.Id == r.ExamSessionId).Select(s => s.Student.FirstName + " " + s.Student.LastName).First(),
                    Exam = _db.ExamSessions.Where(s => s.Id == r.ExamSessionId).Select(s => s.Exam.Title).First(),
                    Ended = _db.ExamSessions.Where(s => s.Id == r.ExamSessionId).Select(s => s.EndTime).First()
                }).ToListAsync();

            return rows.Select(r => new ResumeRowDto
            {
                Id = r.Id, SessionId = r.ExamSessionId, StudentName = r.Student, ExamTitle = r.Exam,
                Reason = r.Reason, Status = r.Status, Note = r.Note,
                DisconnectedAt = r.Ended ?? r.CreatedAt, CreatedAt = r.CreatedAt,
                SecondsLeftToDecide = Math.Max(0, (int)(r.CreatedAt + DecideWithin - now).TotalSeconds)
            }).ToList();
        }

        public async Task<ResumeRowDto> DecideAsync(int actorId, int requestId, ResumeDecisionDto dto)
        {
            var (actor, role, scope) = await ScopeAsync(actorId);

            if (dto.Decision is not ("Approved" or "Rejected")) throw new BadRequestException("Decision must be Approved or Rejected");
            var note = dto.Note?.Trim();
            if (note is { Length: > 500 }) throw new BadRequestException("The note is too long");
            if (dto.Decision == "Rejected" && (note == null || note.Length < 3)) throw new BadRequestException("Add a note for the student");

            var req = await scope.FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new NotFoundException("Request not found");
            if (req.Status != "Pending") throw new ConflictException("This request was already answered");

            var s = await _db.ExamSessions.Include(x => x.Exam).Include(x => x.Student)
                .FirstAsync(x => x.Id == req.ExamSessionId);

            var now = DateTime.UtcNow;
            if (dto.Decision == "Approved")
            {
                if (now - req.CreatedAt > DecideWithin) throw new BadRequestException("The decision window has passed");
                if (s.Status != "Terminated" || s.EndReason != "lost-contact") throw new BadRequestException("This exam can't be resumed");
                if (now >= Deadline(s, s.Exam)) throw new BadRequestException("The exam time is over");

                // فتح الجلسة من جديد: الوقت الأصلي ما بيتغيرش
                s.Status = "InProgress";
                s.EndTime = null;
                s.SubmittedAt = null;
                s.EndReason = null;
                s.TotalScore = 0;
                s.Percentage = 0;
                s.IsPassed = false;

                // التقييم اللي اتعمل وقت الإنهاء يتلغي، والتصحيح يتعاد عند التسليم
                foreach (var a in await _db.Answers.Where(a => a.ExamSessionId == s.Id).ToListAsync())
                {
                    a.IsCorrect = false; a.MarksObtained = 0; a.AISimilarityScore = null; a.AIFeedback = null;
                }

                _db.AILogs.Add(new AILog { ExamSessionId = s.Id, EventType = "ResumedAfterDisconnect", Timestamp = now, ScreenshotPath = "", Confidence = 1 });
                s.RiskScore += RiskCalculator.WeightOf("ResumedAfterDisconnect");
                s.ViolationCount += 1;
                ExamPresence.Touch(s.Id);
            }

            req.Status = dto.Decision;
            req.Note = note;
            req.DecidedAt = now;
            req.DecidedBy = actor.Id;

            _audit.Stage(new AuditEntry
            {
                ActorId = actor.Id, ActorRole = role, FacultyId = s.Student.FacultyId,
                Action = dto.Decision == "Approved" ? ActivityActions.ResumeApproved : ActivityActions.ResumeRejected,
                EntityType = ActivityEntities.Resume, EntityId = s.Id,
                Description = $"{dto.Decision} resume of {s.Student.FirstName} {s.Student.LastName} in \"{s.Exam.Title}\"",
                NewValues = new { req.Status, req.Note }
            });

            await _db.SaveChangesAsync();

            try
            {
                await _notifications.SendNotificationAsync(s.StudentId,
                    dto.Decision == "Approved" ? $"You can continue \"{s.Exam.Title}\" now" : $"Your resume request for \"{s.Exam.Title}\" was rejected", "Resume");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Resume decision notification failed"); }

            return new ResumeRowDto
            {
                Id = req.Id, SessionId = s.Id, StudentName = $"{s.Student.FirstName} {s.Student.LastName}", ExamTitle = s.Exam.Title,
                Reason = req.Reason, Status = req.Status, Note = req.Note, CreatedAt = req.CreatedAt
            };
        }
    }
}
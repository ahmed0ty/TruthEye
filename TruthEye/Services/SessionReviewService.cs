using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Exceptions;
using TruthEye.Security;

namespace TruthEye.Services
{
    public interface ISessionReviewService
    {
        Task<PagedResult<ReviewRowDto>> ListAsync(int actorId, ReviewFilter f);
        Task<ReviewDetailDto> GetAsync(int actorId, int sessionId);
        Task<ReviewDetailDto> DecideAsync(int actorId, int sessionId, ReviewDecisionDto dto);
    }

    public class SessionReviewService : ISessionReviewService
    {
        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;

        public SessionReviewService(AppDbContext db, ISystemLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        // الدكتور: جلسات امتحاناته. الباقي: جلسات طلاب كليته. الدور بيتحدد من نوع الحساب مش من التوكن.
        private async Task<(IQueryable<ExamSession> Scope, User Actor, string Role, int Faculty)> ScopeAsync(int actorId)
        {
            var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId && !u.IsDeleted);
            if (actor == null || !actor.IsActive) throw new ForbiddenException("Your account is disabled");

            var role = StaffRules.RoleOf(actor);
            if (role is not (AppRoles.Doctor or AppRoles.Proctor or AppRoles.Dean or AppRoles.Secretary or AppRoles.Technical))
                throw new ForbiddenException();

            var faculty = ChatRules.FacultyOf(actor);
            var sessions = _db.ExamSessions.Where(s => s.Status != "InProgress" && !s.Exam.IsDeleted);

            if (role == AppRoles.Doctor)
                return (sessions.Where(s => s.Exam.DoctorId == actorId), actor, role, faculty);

            if (faculty <= 0) throw new ForbiddenException("No faculty is assigned to your account");
            return (sessions.Where(s => s.Student.FacultyId == faculty), actor, role, faculty);
        }

        public async Task<PagedResult<ReviewRowDto>> ListAsync(int actorId, ReviewFilter f)
        {
            var (scope, _, _, _) = await ScopeAsync(actorId);
            if (f.ExamId is int eid) scope = scope.Where(s => s.ExamId == eid);

            // الترتيب والفلترة والترقيم كلهم في الداتابيز. الدرجة والعدد مخزنين على الجلسة (بيتحدّثوا مع كل حدث كشف)
            var rows = scope.AsNoTracking().Select(s => new
            {
                s.Id,
                Student = s.Student.FirstName + " " + s.Student.LastName,
                Exam = s.Exam.Title,
                s.Status,
                Submitted = s.SubmittedAt ?? s.EndTime ?? s.StartTime,
                s.RiskScore,
                s.ViolationCount,
                Decision = _db.SessionReviews.Where(r => r.ExamSessionId == s.Id).Select(r => r.Decision).FirstOrDefault()
            });

            if (!f.IncludeClean)
                rows = rows.Where(r => r.ViolationCount > 0 || (r.Decision != null && r.Decision != "Pending"));

            if (!string.IsNullOrEmpty(f.Decision))
                rows = f.Decision == "Pending"
                    ? rows.Where(r => r.Decision == null || r.Decision == "Pending")
                    : rows.Where(r => r.Decision == f.Decision);

            if (!string.IsNullOrEmpty(f.Level))
                rows = f.Level switch
                {
                    "High" => rows.Where(r => r.RiskScore >= RiskCalculator.HighFrom),
                    "Medium" => rows.Where(r => r.RiskScore >= RiskCalculator.MediumFrom && r.RiskScore < RiskCalculator.HighFrom),
                    "Low" => rows.Where(r => r.RiskScore > 0 && r.RiskScore < RiskCalculator.MediumFrom),
                    "None" => rows.Where(r => r.RiskScore == 0),
                    _ => rows.Where(r => false)
                };

            var total = await rows.CountAsync();
            var size = Math.Clamp(f.PageSize, 1, 100);
            var page = Math.Max(1, f.Page);

            var pageRows = await rows
                .OrderByDescending(r => r.RiskScore)
                .ThenByDescending(r => r.Submitted)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            // عدد الأدلة (لقطات الشاشة) بيتحسب للصفحة الحالية بس
            var ids = pageRows.Select(r => r.Id).ToList();
            var evidence = await _db.AILogs.AsNoTracking()
                .Where(l => ids.Contains(l.ExamSessionId) && l.ScreenshotPath != null && l.ScreenshotPath != "")
                .GroupBy(l => l.ExamSessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.Count);

            return new PagedResult<ReviewRowDto>
            {
                Items = pageRows.Select(r => new ReviewRowDto
                {
                    SessionId = r.Id,
                    StudentName = r.Student,
                    ExamTitle = r.Exam,
                    SessionStatus = r.Status,
                    RiskScore = r.RiskScore,
                    RiskLevel = RiskCalculator.Level(r.RiskScore),
                    ViolationCount = r.ViolationCount,
                    EvidenceCount = evidence.GetValueOrDefault(r.Id),
                    Decision = r.Decision ?? "Pending",
                    SubmittedAt = r.Submitted
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = size
            };
        }

        public async Task<ReviewDetailDto> GetAsync(int actorId, int sessionId)
        {
            var (scope, _, role, _) = await ScopeAsync(actorId);

            var s = await scope.AsNoTracking().Where(x => x.Id == sessionId).Select(x => new
            {
                x.Id,
                Student = x.Student.FirstName + " " + x.Student.LastName,
                Exam = x.Exam.Title,
                x.Status,
                x.Percentage,
                x.StartTime,
                x.EndTime,
                Submitted = x.SubmittedAt ?? x.EndTime ?? x.StartTime
            }).FirstOrDefaultAsync() ?? throw new NotFoundException("Session not found");

            var logs = await _db.AILogs.AsNoTracking()
                .Where(l => l.ExamSessionId == sessionId)
                .OrderBy(l => l.Timestamp).ToListAsync();

            var review = await _db.SessionReviews.AsNoTracking().FirstOrDefaultAsync(r => r.ExamSessionId == sessionId);
            var score = RiskCalculator.Score(logs.GroupBy(l => l.EventType).Select(g => ((string?)g.Key, g.Count())));

            return new ReviewDetailDto
            {
                SessionId = s.Id,
                StudentName = s.Student,
                ExamTitle = s.Exam,
                SessionStatus = s.Status,
                Percentage = s.Percentage,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SubmittedAt = s.Submitted,
                RiskScore = score,
                RiskLevel = RiskCalculator.Level(score),
                ViolationCount = logs.Count,
                EvidenceCount = logs.Count(l => !string.IsNullOrEmpty(l.ScreenshotPath)),
                Decision = review?.Decision ?? "Pending",
                Note = review?.Note,
                ReviewedAt = review?.ReviewedAt,
                CanDecide = role != AppRoles.Technical,
                Events = logs.Select(l => new ReviewEventDto
                {
                    Id = l.Id,
                    EventType = l.EventType,
                    Confidence = l.Confidence,
                    Weight = RiskCalculator.WeightOf(l.EventType),
                    Timestamp = l.Timestamp,
                    Evidence = string.IsNullOrEmpty(l.ScreenshotPath) ? null : l.ScreenshotPath
                }).ToList()
            };
        }

        public async Task<ReviewDetailDto> DecideAsync(int actorId, int sessionId, ReviewDecisionDto dto)
        {
            var (scope, actor, role, faculty) = await ScopeAsync(actorId);

            if (role == AppRoles.Technical)
                throw new ForbiddenException("You can view reviews but not decide on them");
            if (dto.Decision is not ("Cleared" or "Confirmed"))
                throw new BadRequestException("Decision must be Cleared or Confirmed");

            var note = dto.Note?.Trim();
            if (note is { Length: > 1000 }) throw new BadRequestException("The note is too long");
            if (dto.Decision == "Confirmed" && (note == null || note.Length < 5))
                throw new BadRequestException("Add a note explaining why the violation is confirmed");

            var session = await scope.Include(x => x.Student).Include(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == sessionId)
                ?? throw new NotFoundException("Session not found");

            var counts = await _db.AILogs.Where(l => l.ExamSessionId == sessionId)
                .GroupBy(l => l.EventType).Select(g => new { g.Key, C = g.Count() }).ToListAsync();
            var score = RiskCalculator.Score(counts.Select(c => ((string?)c.Key, c.C)));

            var review = await _db.SessionReviews.FirstOrDefaultAsync(r => r.ExamSessionId == sessionId);
            var before = review == null ? null : new { review.Decision, review.Note };

            if (review == null)
            {
                review = new SessionReview { ExamSessionId = sessionId };
                _db.SessionReviews.Add(review);
            }
            review.Decision = dto.Decision;
            review.Note = note;
            review.RiskScore = score;
            review.ReviewedBy = actor.Id;
            review.ReviewedAt = DateTime.UtcNow;

            _audit.Stage(new AuditEntry
            {
                ActorId = actor.Id,
                ActorRole = role,
                FacultyId = faculty > 0 ? faculty : ChatRules.FacultyOf(session.Student!),
                Action = dto.Decision == "Confirmed" ? ActivityActions.ReviewConfirmed : ActivityActions.ReviewCleared,
                EntityType = ActivityEntities.SessionReview,
                EntityId = sessionId,
                Description = $"{dto.Decision} session of {session.Student!.FirstName} {session.Student.LastName} in \"{session.Exam!.Title}\" (risk {score})",
                OldValues = before,
                NewValues = new { review.Decision, review.Note, RiskScore = score }
            });

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // قرارين في نفس اللحظة على نفس الجلسة: الـ unique index كسب
                throw new ConflictException("Someone else just reviewed this session. Reload and try again");
            }

            return await GetAsync(actorId, sessionId);
        }
    }
}
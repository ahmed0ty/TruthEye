using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Exceptions;
using TruthEye.Security;

namespace TruthEye.Services
{
    public interface IAppealService
    {
        Task<List<StudentAppealItemDto>> MineAsync(int studentId);
        Task<StudentAppealItemDto> CreateAsync(int studentId, CreateAppealDto dto);
        Task<PagedResult<AppealRowDto>> ListAsync(int doctorId, string? status, int page, int pageSize);
        Task<AppealRowDto> RespondAsync(int doctorId, int appealId, AppealResponseDto dto);
    }

    public class AppealService : IAppealService
    {
        private const int WindowDays = 7;

        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;
        private readonly INotificationService _notifications;
        private readonly ILogger<AppealService> _logger;

        public AppealService(AppDbContext db, ISystemLogService audit, INotificationService notifications, ILogger<AppealService> logger)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _logger = logger;
        }

        private static DateTime Deadline(Exam e) => e.EndTime.AddDays(WindowDays);

        public async Task<List<StudentAppealItemDto>> MineAsync(int studentId)
        {
            var now = DateTime.UtcNow;

            var sessions = await _db.ExamSessions.AsNoTracking()
                .Include(s => s.Exam)
                .Where(s => s.StudentId == studentId && s.Status != "InProgress" && !s.Exam.IsDeleted && s.Exam.EndTime < now)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            var ids = sessions.Select(s => s.Id).ToList();
            var appeals = await _db.Appeals.AsNoTracking()
                .Where(a => ids.Contains(a.ExamSessionId))
                .ToDictionaryAsync(a => a.ExamSessionId);

            return sessions.Select(s =>
            {
                appeals.TryGetValue(s.Id, out var a);
                var deadline = Deadline(s.Exam);
                return new StudentAppealItemDto
                {
                    SessionId = s.Id,
                    ExamTitle = s.Exam.Title,
                    FinishedAt = s.SubmittedAt ?? s.EndTime ?? s.StartTime,
                    Percentage = s.Percentage,
                    IsPassed = s.IsPassed,
                    CanAppeal = a == null && now <= deadline,
                    AppealDeadline = deadline,
                    AppealId = a?.Id,
                    Status = a?.Status,
                    Message = a?.Message,
                    Response = a?.Response,
                    RespondedAt = a?.RespondedAt
                };
            }).ToList();
        }

        public async Task<StudentAppealItemDto> CreateAsync(int studentId, CreateAppealDto dto)
        {
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null || !student.IsActive) throw new ForbiddenException("Your account is disabled");

            var message = dto.Message?.Trim() ?? "";
            if (message.Length < 10) throw new BadRequestException("Please explain your appeal in at least 10 characters");
            if (message.Length > 1000) throw new BadRequestException("The message is too long");

            var session = await _db.ExamSessions.Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == dto.ExamSessionId && s.StudentId == studentId && !s.Exam.IsDeleted)
                ?? throw new NotFoundException("Exam session not found");

            var now = DateTime.UtcNow;
            if (session.Status == "InProgress") throw new BadRequestException("The exam is still in progress");
            if (now <= session.Exam.EndTime) throw new BadRequestException("You can appeal after the exam time ends");
            if (now > Deadline(session.Exam)) throw new BadRequestException($"The appeal period ({WindowDays} days) has ended");
            if (await _db.Appeals.AnyAsync(a => a.ExamSessionId == session.Id))
                throw new ConflictException("You already submitted an appeal for this exam");

            _db.Appeals.Add(new Appeal
            {
                ExamSessionId = session.Id,
                StudentId = studentId,
                Message = message,
                Status = "Pending",
                CreatedAt = now
            });

            _audit.Stage(new AuditEntry
            {
                ActorId = studentId,
                ActorRole = AppRoles.Student,
                FacultyId = student.FacultyId,
                Action = ActivityActions.AppealCreated,
                EntityType = ActivityEntities.Appeal,
                EntityId = session.Id,
                Description = $"{student.FirstName} {student.LastName} appealed the result of \"{session.Exam.Title}\""
            });

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new ConflictException("You already submitted an appeal for this exam"); }

            try
            {
                await _notifications.SendNotificationAsync(session.Exam.DoctorId,
                    $"New appeal on \"{session.Exam.Title}\" from {student.FirstName} {student.LastName}", "Appeal");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Appeal notification failed"); }

            return (await MineAsync(studentId)).First(i => i.SessionId == session.Id);
        }

        public async Task<PagedResult<AppealRowDto>> ListAsync(int doctorId, string? status, int page, int pageSize)
        {
            var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);
            if (doctor == null || !doctor.IsActive) throw new ForbiddenException("Your account is disabled");

            var q = _db.Appeals.AsNoTracking()
                .Where(a => !a.ExamSession.Exam.IsDeleted && a.ExamSession.Exam.DoctorId == doctorId);

            if (status is "Pending" or "Accepted" or "Rejected") q = q.Where(a => a.Status == status);

            var size = Math.Clamp(pageSize, 1, 100);
            var p = Math.Max(1, page);
            var total = await q.CountAsync();

            var items = await q.OrderBy(a => a.Status == "Pending" ? 0 : 1).ThenByDescending(a => a.CreatedAt)
                .Skip((p - 1) * size).Take(size)
                .Select(a => new AppealRowDto
                {
                    Id = a.Id,
                    SessionId = a.ExamSessionId,
                    StudentName = a.ExamSession.Student.FirstName + " " + a.ExamSession.Student.LastName,
                    ExamTitle = a.ExamSession.Exam.Title,
                    Percentage = a.ExamSession.Percentage,
                    Message = a.Message,
                    Status = a.Status,
                    Response = a.Response,
                    CreatedAt = a.CreatedAt,
                    RespondedAt = a.RespondedAt
                }).ToListAsync();

            return new PagedResult<AppealRowDto> { Items = items, Total = total, Page = p, PageSize = size };
        }

        public async Task<AppealRowDto> RespondAsync(int doctorId, int appealId, AppealResponseDto dto)
        {
            var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);
            if (doctor == null || !doctor.IsActive) throw new ForbiddenException("Your account is disabled");

            if (dto.Decision is not ("Accepted" or "Rejected"))
                throw new BadRequestException("Decision must be Accepted or Rejected");
            var response = dto.Response?.Trim() ?? "";
            if (response.Length < 5) throw new BadRequestException("Write a response for the student");
            if (response.Length > 1000) throw new BadRequestException("The response is too long");

            var appeal = await _db.Appeals
                .Include(a => a.ExamSession).ThenInclude(s => s.Exam)
                .Include(a => a.ExamSession).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == appealId)
                ?? throw new NotFoundException("Appeal not found");

            var session = appeal.ExamSession;
            if (session.Exam.IsDeleted || session.Exam.DoctorId != doctorId)
                throw new NotFoundException("Appeal not found");
            if (appeal.Status != "Pending")
                throw new ConflictException("This appeal was already answered");

            appeal.Status = dto.Decision;
            appeal.Response = response;
            appeal.RespondedAt = DateTime.UtcNow;
            appeal.RespondedBy = doctorId;

            _audit.Stage(new AuditEntry
            {
                ActorId = doctorId,
                ActorRole = AppRoles.Doctor,
                FacultyId = session.Student.FacultyId,
                Action = dto.Decision == "Accepted" ? ActivityActions.AppealAccepted : ActivityActions.AppealRejected,
                EntityType = ActivityEntities.Appeal,
                EntityId = session.Id,
                Description = $"{dto.Decision} the appeal of {session.Student.FirstName} {session.Student.LastName} on \"{session.Exam.Title}\"",
                NewValues = new { appeal.Status, appeal.Response }
            });

            await _db.SaveChangesAsync();

            try
            {
                await _notifications.SendNotificationAsync(session.StudentId,
                    $"Your appeal on \"{session.Exam.Title}\" was {dto.Decision.ToLowerInvariant()}", "Appeal");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Appeal response notification failed"); }

            return new AppealRowDto
            {
                Id = appeal.Id,
                SessionId = session.Id,
                StudentName = $"{session.Student.FirstName} {session.Student.LastName}",
                ExamTitle = session.Exam.Title,
                Percentage = session.Percentage,
                Message = appeal.Message,
                Status = appeal.Status,
                Response = appeal.Response,
                CreatedAt = appeal.CreatedAt,
                RespondedAt = appeal.RespondedAt
            };
        }
    }
}
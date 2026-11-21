using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Exceptions;
using TruthEye.Security;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class BulkEnrollRequest
    {
        /// ids picked from the table
        public List<int>? StudentIds { get; set; }

        /// emails or national ids pasted by the admin (one item each)
        public List<string>? Identifiers { get; set; }

        /// optional: everyone in this department and/or year
        public int? DepartmentId { get; set; }
        public int? Year { get; set; }
    }

    public class BulkEnrollResult
    {
        public int Matched { get; set; }
        public int Added { get; set; }
        public List<string> AlreadyEnrolled { get; set; } = new();
        public List<string> NotFound { get; set; } = new();
    }

    public interface ICourseEnrollmentService
    {
        Task<BulkEnrollResult> EnrollAsync(int courseId, BulkEnrollRequest request, int actorId);
        Task<int> UnenrollAsync(int courseId, List<int> studentIds, int actorId);
    }

    public class CourseEnrollmentService : ICourseEnrollmentService
    {
        private const int MaxStudents = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ISystemLogService _audit;
        private readonly ILogger<CourseEnrollmentService> _logger;

        public CourseEnrollmentService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ISystemLogService audit,
            ILogger<CourseEnrollmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _audit = audit;
            _logger = logger;
        }

        private async Task<Admin> ActorAsync(int actorId)
        {
            var actor = await _unitOfWork.Repository<Admin>().GetByIdAsync(actorId);
            if (actor == null || !actor.IsActive) throw new ForbiddenException("Your account is disabled");
            return actor;
        }

        private async Task<Course> CourseOfAsync(int courseId, int facultyId)
        {
            var course = await _unitOfWork.Repository<Course>()
                .GetByIdAsync(courseId, c => c.Department, c => c.RegisteredStudents);
            // كورس كلية تانية = "مش موجود"
            if (course?.Department == null || course.Department.FacultyId != facultyId)
                throw new NotFoundException("Course not found");
            return course;
        }

        private static AuditEntry Entry(Admin actor, string action, Course course, string text, object? oldV, object? newV) => new()
        {
            ActorId = actor.Id,
            ActorRole = StaffRules.RoleOf(actor),
            FacultyId = actor.FacultyId,
            Action = action,
            EntityType = ActivityEntities.CourseStudent,
            EntityId = course.Id,
            Description = text,
            OldValues = oldV,
            NewValues = newV
        };

        public async Task<BulkEnrollResult> EnrollAsync(int courseId, BulkEnrollRequest request, int actorId)
        {
            var actor = await ActorAsync(actorId);
            var fid = actor.FacultyId;
            var course = await CourseOfAsync(courseId, fid);

            var result = new BulkEnrollResult();
            var students = new Dictionary<int, Student>();

            // كل الاستعلامات متقيّدة بكلية المنفّذ وبالطلاب النشطين بس

            // 1) ids picked from the table
            if (request.StudentIds is { Count: > 0 })
            {
                var ids = request.StudentIds.Distinct().Take(MaxStudents + 1).ToList();
                var found = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => ids.Contains(s.Id) && s.FacultyId == fid && s.IsActive);
                foreach (var s in found) students[s.Id] = s;
                result.NotFound.AddRange(ids.Where(i => !students.ContainsKey(i)).Select(i => $"#{i}"));
            }

            // 2) emails / national ids
            if (request.Identifiers is { Count: > 0 })
            {
                var wanted = request.Identifiers
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxStudents + 1)
                    .ToList();

                var found = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => s.FacultyId == fid && s.IsActive
                                    && (wanted.Contains(s.Email) || wanted.Contains(s.NationalId)));

                var hit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in found)
                {
                    students[s.Id] = s;
                    hit.Add(s.Email ?? string.Empty);
                    hit.Add(s.NationalId ?? string.Empty);
                }
                result.NotFound.AddRange(wanted.Where(w => !hit.Contains(w)));
            }

            // 3) everyone in a department and/or year (داخل الكلية بس)
            if (request.DepartmentId.HasValue || request.Year.HasValue)
            {
                var dep = request.DepartmentId;
                var yr = request.Year;
                var found = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => s.FacultyId == fid && s.IsActive
                                    && (dep == null || s.DepartmentId == dep)
                                    && (yr == null || (int)s.Year == yr));
                foreach (var s in found) students[s.Id] = s;
            }

            if (students.Count == 0 && result.NotFound.Count == 0)
                throw new BadRequestException("No students were selected.");
            if (students.Count > MaxStudents)
                throw new BadRequestException($"Too many students ({students.Count}). The maximum per request is {MaxStudents}. Narrow the selection.");

            var enrolled = course.RegisteredStudents.Select(s => s.Id).ToHashSet();
            var toAdd = students.Values.Where(s => !enrolled.Contains(s.Id)).ToList();

            result.Matched = students.Count;
            result.AlreadyEnrolled.AddRange(
                students.Values.Where(s => enrolled.Contains(s.Id)).Select(s => s.Email ?? s.NationalId));
            result.Added = toAdd.Count;

            if (toAdd.Count > 0)
            {
                foreach (var s in toAdd) course.RegisteredStudents.Add(s);

                _audit.Stage(Entry(actor, ActivityActions.BulkAssign, course,
                    $"Added {toAdd.Count} students to course {course.Name}",
                    null, new { Ids = toAdd.Select(s => s.Id).ToArray() }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateCachesAsync(course, fid, toAdd.Select(s => s.Id));
            }

            _logger.LogInformation(
                "Bulk enroll into course {CourseId} by {ActorId}: {Added} added, {Already} already enrolled, {Missing} not found",
                courseId, actorId, result.Added, result.AlreadyEnrolled.Count, result.NotFound.Count);

            return result;
        }

        public async Task<int> UnenrollAsync(int courseId, List<int> studentIds, int actorId)
        {
            var actor = await ActorAsync(actorId);
            var course = await CourseOfAsync(courseId, actor.FacultyId);

            var ids = studentIds.Distinct().Take(MaxStudents).ToHashSet();
            var toRemove = course.RegisteredStudents.Where(s => ids.Contains(s.Id)).ToList();
            foreach (var s in toRemove) course.RegisteredStudents.Remove(s);

            if (toRemove.Count > 0)
            {
                _audit.Stage(Entry(actor, ActivityActions.Unassign, course,
                    $"Removed {toRemove.Count} students from course {course.Name}",
                    new { Ids = toRemove.Select(s => s.Id).ToArray() }, null));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateCachesAsync(course, actor.FacultyId, toRemove.Select(s => s.Id));
            }

            return toRemove.Count;
        }

        // نفس المفاتيح اللي CourseService بيقرا منها (فيها رقم الكلية)
        private async Task InvalidateCachesAsync(Course course, int facultyId, IEnumerable<int> studentIds)
        {
            await _cacheService.RemoveAsync($"courses_{facultyId}_{course.Id}");
            await _cacheService.RemoveAsync($"course_students_{facultyId}_{course.Id}");
            await _cacheService.RemoveAsync($"faculty_courses_{facultyId}");
            await _cacheService.RemoveAsync($"department_courses_{course.DepartmentId}");

            foreach (var id in studentIds)
            {
                await _cacheService.RemoveAsync($"available-exams:{id}");
                await _cacheService.RemoveAsync($"student_courses_{id}");
            }
        }
    }
}
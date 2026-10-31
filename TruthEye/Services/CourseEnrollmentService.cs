using TruthEye.Entities;
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
        Task<BulkEnrollResult> EnrollAsync(int courseId, BulkEnrollRequest request);
        Task<int> UnenrollAsync(int courseId, List<int> studentIds);
    }

    public class CourseEnrollmentService : ICourseEnrollmentService
    {
        private const int MaxStudents = 1000;

        // Same cache keys that CourseService reads from (see CourseService.cs)
        private const string COURSES_CACHE_PREFIX = "courses_";
        private const string FACULTY_COURSES_CACHE_PREFIX = "faculty_courses_";
        private const string COURSE_STUDENTS_CACHE_PREFIX = "course_students_";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CourseEnrollmentService> _logger;

        public CourseEnrollmentService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<CourseEnrollmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<BulkEnrollResult> EnrollAsync(int courseId, BulkEnrollRequest request)
        {
            var course = await _unitOfWork.Repository<Course>()
                .GetByIdAsync(courseId, c => c.RegisteredStudents);
            if (course == null)
                throw new Exception("Course not found");

            var result = new BulkEnrollResult();
            var students = new Dictionary<int, Student>();

            // 1) ids picked from the table
            if (request.StudentIds is { Count: > 0 })
            {
                var ids = request.StudentIds.Distinct().ToList();
                var found = await _unitOfWork.Repository<Student>().FindAsync(s => ids.Contains(s.Id));
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
                    .ToList();

                var found = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => wanted.Contains(s.Email) || wanted.Contains(s.NationalId));

                var hit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in found)
                {
                    students[s.Id] = s;
                    hit.Add(s.Email ?? string.Empty);
                    hit.Add(s.NationalId ?? string.Empty);
                }
                result.NotFound.AddRange(wanted.Where(w => !hit.Contains(w)));
            }

            // 3) everyone in a department and/or year
            if (request.DepartmentId.HasValue || request.Year.HasValue)
            {
                var dep = request.DepartmentId;
                var yr = request.Year;
                var found = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => (dep == null || s.DepartmentId == dep) && (yr == null || (int)s.Year == yr));
                foreach (var s in found) students[s.Id] = s;
            }

            if (students.Count == 0 && result.NotFound.Count == 0)
                throw new Exception("No students were selected.");
            if (students.Count > MaxStudents)
                throw new Exception($"Too many students ({students.Count}). The maximum per request is {MaxStudents}. Narrow the selection.");

            // everything is saved in one go
            var enrolled = course.RegisteredStudents.Select(s => s.Id).ToHashSet();
            var toAdd = students.Values.Where(s => !enrolled.Contains(s.Id)).ToList();

            result.Matched = students.Count;
            result.AlreadyEnrolled.AddRange(
    students.Values
        .Where(s => enrolled.Contains(s.Id))
        .Select(s => s.Email ?? s.NationalId)
);
            result.Added = toAdd.Count;

            if (toAdd.Count > 0)
            {
                foreach (var s in toAdd) course.RegisteredStudents.Add(s);
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateCachesAsync(course, toAdd.Select(s => s.Id));
            }

            _logger.LogInformation(
                "Bulk enroll into course {CourseId}: {Added} added, {Already} already enrolled, {Missing} not found",
                courseId, result.Added, result.AlreadyEnrolled, result.NotFound.Count);

            return result;
        }

        public async Task<int> UnenrollAsync(int courseId, List<int> studentIds)
        {
            var course = await _unitOfWork.Repository<Course>()
                .GetByIdAsync(courseId, c => c.RegisteredStudents);
            if (course == null)
                throw new Exception("Course not found");

            var ids = studentIds.Distinct().ToHashSet();
            var toRemove = course.RegisteredStudents.Where(s => ids.Contains(s.Id)).ToList();
            foreach (var s in toRemove) course.RegisteredStudents.Remove(s);

            if (toRemove.Count > 0)
            {
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateCachesAsync(course, toRemove.Select(s => s.Id));
            }

            return toRemove.Count;
        }

        // Clears every cached copy that includes the course's student list, so the change shows up right away
        private async Task InvalidateCachesAsync(Course course, IEnumerable<int> studentIds)
        {
            await _cacheService.RemoveAsync($"{COURSE_STUDENTS_CACHE_PREFIX}{course.Id}");
            await _cacheService.RemoveAsync($"{COURSES_CACHE_PREFIX}{course.Id}");

            // The faculty course list shows student counts, and is cached per faculty
            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(course.DepartmentId);
            if (department != null)
            {
                await _cacheService.RemoveAsync($"{FACULTY_COURSES_CACHE_PREFIX}{department.FacultyId}");
                await _cacheService.RemoveAsync($"department_courses_{department.Id}");
            }

            foreach (var id in studentIds)
            {
                await _cacheService.RemoveAsync($"available-exams:{id}");   // exams they can now (or no longer) see
                await _cacheService.RemoveAsync($"student_courses_{id}");   // their course list
            }
        }
    }
}
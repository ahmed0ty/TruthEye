using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CourseService> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationService _notifications;
        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;

        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        // كل المفاتيح فيها رقم الكلية، فكلية مبتشوفش كاش كلية تانية
        private static string CourseKey(int faculty, int course) => $"courses_{faculty}_{course}";
        private static string FacultyCoursesKey(int faculty) => $"faculty_courses_{faculty}";
        private static string CourseDoctorsKey(int faculty, int course) => $"course_doctors_{faculty}_{course}";
        private static string CourseStudentsKey(int faculty, int course) => $"course_students_{faculty}_{course}";

        public CourseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CourseService> logger,
            ICacheService cacheService,
            INotificationService notifications,
            AppDbContext db,
            ISystemLogService audit)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _notifications = notifications;
            _db = db;
            _audit = audit;
        }

        #region Course Management

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto, int adminId)
        {
            try
            {
                Validate(createCourseDto);

                var admin = await GetAdminWithFaculty(adminId);

                var department = await _unitOfWork.Repository<Department>()
                    .GetByIdAsync(createCourseDto.DepartmentId, d => d.Faculty);

                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new Exception("Department does not belong to your faculty");

                var course = _mapper.Map<Course>(createCourseDto);
                course.CreatedBy = admin.Id;

                await _unitOfWork.Repository<Course>().AddAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await _audit.LogAsync(Entry(admin, ActivityActions.Create, ActivityEntities.Course, course.Id,
                    $"Created course {course.Name}", null, CourseSnapshot(course)));

                await ClearFacultyCoursesCache(admin.FacultyId);
                await ClearDepartmentCoursesCache(createCourseDto.DepartmentId);

                var createdCourse = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(course.Id, c => c.Department);

                return _mapper.Map<CourseDto>(createdCourse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course by Admin {AdminId}", adminId);
                throw new Exception($"Error creating course: {ex.Message}");
            }
        }

        public async Task<CourseDto> UpdateCourseAsync(int courseId, UpdateCourseDto updateCourseDto, int adminId)
        {
            try
            {
                Validate(updateCourseDto);

                var admin = await GetAdminWithFaculty(adminId);

                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(courseId, c => c.Department);

                if (course == null || course.Department.FacultyId != admin.FacultyId)
                    throw new Exception("Course not found or access denied");

                var oldDepartmentId = course.DepartmentId;

                if (updateCourseDto.DepartmentId != oldDepartmentId)
                {
                    var newDepartment = await _unitOfWork.Repository<Department>()
                        .GetByIdAsync(updateCourseDto.DepartmentId);

                    if (newDepartment == null || newDepartment.FacultyId != admin.FacultyId)
                        throw new Exception("New department does not belong to your faculty");
                }

                var before = CourseSnapshot(course);

                course.Name = updateCourseDto.Name;
                course.Description = updateCourseDto.Description;
                course.DepartmentId = updateCourseDto.DepartmentId;
                course.UpdatedAt = DateTime.UtcNow;
                course.UpdatedBy = admin.Id;

                _audit.Stage(Entry(admin, ActivityActions.Update, ActivityEntities.Course, course.Id,
                    $"Updated course {course.Name}", before, CourseSnapshot(course)));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearDepartmentCoursesCache(oldDepartmentId);
                await ClearDepartmentCoursesCache(updateCourseDto.DepartmentId);

                return _mapper.Map<CourseDto>(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId} by Admin {AdminId}", courseId, adminId);
                throw new Exception($"Error updating course: {ex.Message}");
            }
        }

        public async Task<bool> DeleteCourseAsync(int courseId, int adminId)
        {
            try
            {
                var admin = await GetAdminWithFaculty(adminId);

                if (!StaffRules.CanDeleteMembers(StaffRules.RoleOf(admin)))
                    throw new Exception("You don't have permission to delete courses");

                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(courseId, c => c.Department);

                if (course == null || course.Department.FacultyId != admin.FacultyId)
                    throw new Exception("Course not found or access denied");

                _audit.Stage(Entry(admin, ActivityActions.Delete, ActivityEntities.Course, course.Id,
                    $"Deleted course {course.Name}", CourseSnapshot(course)));

                await _unitOfWork.Repository<Course>().DeleteAsync(course);
                course.DeletedBy = admin.Id;
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearDepartmentCoursesCache(course.DepartmentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId} by Admin {AdminId}", courseId, adminId);
                throw new Exception($"Error deleting course: {ex.Message}");
            }
        }

        public async Task<CourseDto> GetCourseByIdAsync(int courseId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);   // الفحص الأول، قبل الكاش

            return await _cacheService.GetOrSetAsync(CourseKey(admin.FacultyId, courseId), async () =>
            {
                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(courseId,
                        c => c.Department,
                        c => c.Instructors,
                        c => c.RegisteredStudents);

                if (course == null || course.Department.FacultyId != admin.FacultyId)
                    throw new Exception("Course not found or access denied");

                return _mapper.Map<CourseDto>(course);
            }, _cacheExpiration);
        }

        public async Task<List<CourseDto>> GetFacultyCoursesAsync(int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            return await _cacheService.GetOrSetAsync(FacultyCoursesKey(admin.FacultyId), async () =>
            {
                var departments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.FacultyId == admin.FacultyId);

                var departmentIds = departments.Select(d => d.Id).ToList();

                var courses = await _unitOfWork.Repository<Course>()
                    .FindAsync(c => departmentIds.Contains(c.DepartmentId),
                        c => c.Department,
                        c => c.Instructors,
                        c => c.RegisteredStudents);

                return _mapper.Map<List<CourseDto>>(courses);
            }, _cacheExpiration);
        }

        #endregion

        #region Doctor Management in Courses

        public async Task<bool> AssignDoctorToCourseAsync(int courseId, int doctorId, int adminId)
        {
            try
            {
                var admin = await GetAdminWithFaculty(adminId);

                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                var doctor = await GetDoctorWithPermissions(doctorId, admin.FacultyId);

                if (course.Instructors.Any(d => d.Id == doctorId))
                    throw new Exception("Doctor is already assigned to this course");

                course.Instructors.Add(doctor);

                _audit.Stage(Entry(admin, ActivityActions.Assign, ActivityEntities.CourseDoctor, course.Id,
                    $"Assigned doctor {doctor.FirstName} {doctor.LastName} to course {course.Name}",
                    null, new { Ids = new[] { doctorId } }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await _notifications.SendNotificationAsync(doctorId, $"You were assigned to course: {course.Name}", "Course");

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearDoctorCoursesCache(doctorId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning doctor {DoctorId} to course {CourseId}", doctorId, courseId);
                throw new Exception($"Error assigning doctor to course: {ex.Message}");
            }
        }

        public async Task<bool> RemoveDoctorFromCourseAsync(int courseId, int doctorId, int adminId)
        {
            try
            {
                var admin = await GetAdminWithFaculty(adminId);

                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                var doctor = await GetDoctorWithPermissions(doctorId, admin.FacultyId);

                if (!course.Instructors.Any(d => d.Id == doctorId))
                    throw new Exception("Doctor is not assigned to this course");

                course.Instructors.Remove(doctor);

                _audit.Stage(Entry(admin, ActivityActions.Unassign, ActivityEntities.CourseDoctor, course.Id,
                    $"Removed doctor {doctor.FirstName} {doctor.LastName} from course {course.Name}",
                    new { Ids = new[] { doctorId } }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearDoctorCoursesCache(doctorId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing doctor {DoctorId} from course {CourseId}", doctorId, courseId);
                throw new Exception($"Error removing doctor from course: {ex.Message}");
            }
        }

        public async Task<List<DoctorDto>> GetCourseDoctorsAsync(int courseId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            return await _cacheService.GetOrSetAsync(CourseDoctorsKey(admin.FacultyId, courseId), async () =>
            {
                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                return _mapper.Map<List<DoctorDto>>(course.Instructors.Where(d => !d.IsDeleted).ToList());
            }, _cacheExpiration);
        }

        #endregion

        #region Student Management in Courses

        public async Task<bool> AssignStudentToCourseAsync(int courseId, int studentId, int adminId)
        {
            try
            {
                var admin = await GetAdminWithFaculty(adminId);

                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                var student = await GetStudentWithPermissions(studentId, admin.FacultyId);

                if (course.RegisteredStudents.Any(s => s.Id == studentId))
                    throw new Exception("Student is already registered in this course");

                course.RegisteredStudents.Add(student);

                _audit.Stage(Entry(admin, ActivityActions.Assign, ActivityEntities.CourseStudent, course.Id,
                    $"Added student {student.FirstName} {student.LastName} to course {course.Name}",
                    null, new { Ids = new[] { studentId } }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await _notifications.SendNotificationAsync(studentId, $"You were added to course: {course.Name}", "Course");

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearStudentCoursesCache(studentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning student {StudentId} to course {CourseId}", studentId, courseId);
                throw new Exception($"Error assigning student to course: {ex.Message}");
            }
        }

        public async Task<bool> RemoveStudentFromCourseAsync(int courseId, int studentId, int adminId)
        {
            try
            {
                var admin = await GetAdminWithFaculty(adminId);

                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                var student = await GetStudentWithPermissions(studentId, admin.FacultyId);

                if (!course.RegisteredStudents.Any(s => s.Id == studentId))
                    throw new Exception("Student is not registered in this course");

                course.RegisteredStudents.Remove(student);

                _audit.Stage(Entry(admin, ActivityActions.Unassign, ActivityEntities.CourseStudent, course.Id,
                    $"Removed student {student.FirstName} {student.LastName} from course {course.Name}",
                    new { Ids = new[] { studentId } }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCaches(admin.FacultyId, courseId);
                await ClearStudentCoursesCache(studentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing student {StudentId} from course {CourseId}", studentId, courseId);
                throw new Exception($"Error removing student from course: {ex.Message}");
            }
        }

        public async Task<List<StudentDto>> GetCourseStudentsAsync(int courseId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            return await _cacheService.GetOrSetAsync(CourseStudentsKey(admin.FacultyId, courseId), async () =>
            {
                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);
                return _mapper.Map<List<StudentDto>>(course.RegisteredStudents.Where(s => !s.IsDeleted).ToList());
            }, _cacheExpiration);
        }

        // Adds many students at once. Each item is an email or a national ID.
        public async Task<BulkEnrollResult> AssignStudentsBulkAsync(int courseId, List<string> identifiers, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);
            var course = await GetCourseWithPermissions(courseId, admin.FacultyId);

            var wanted = identifiers
                .Select(i => (i ?? "").Trim())
                .Where(i => i.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2000)
                .ToList();

            // Only active students of this admin's faculty can be enrolled
            var students = (await _unitOfWork.Repository<Student>()
                .FindAsync(s => s.FacultyId == admin.FacultyId && s.IsActive)).ToList();

            var result = new BulkEnrollResult();
            var enrolledIds = course.RegisteredStudents.Select(s => s.Id).ToHashSet();
            var newlyAdded = new List<Student>();

            foreach (var id in wanted)
            {
                var student = students.FirstOrDefault(s =>
                    string.Equals(s.Email, id, StringComparison.OrdinalIgnoreCase) || s.NationalId == id);

                if (student == null) { result.NotFound.Add(id); continue; }
                if (!enrolledIds.Add(student.Id)) { result.AlreadyEnrolled.Add(id); continue; }

                course.RegisteredStudents.Add(student);
                newlyAdded.Add(student);
            }

            if (newlyAdded.Count > 0)
            {
                _audit.Stage(Entry(admin, ActivityActions.BulkAssign, ActivityEntities.CourseStudent, course.Id,
                    $"Added {newlyAdded.Count} students to course {course.Name}",
                    null, new { Ids = newlyAdded.Select(s => s.Id).ToArray() }));

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCaches(admin.FacultyId, courseId);
                foreach (var s in newlyAdded)
                {
                    await ClearStudentCoursesCache(s.Id);
                    // so the course exams show up for the student right away
                    await _cacheService.RemoveAsync($"available-exams:{s.Id}");
                }

                // notifications are best effort and never undo the enrollment
                foreach (var s in newlyAdded)
                {
                    try { await _notifications.SendNotificationAsync(s.Id, $"You were added to course: {course.Name}", "Course"); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Notification failed for student {StudentId}", s.Id); }
                }
            }

            result.Added = newlyAdded.Count;
            return result;
        }

        #endregion

        #region Revert

        public async Task<string> RevertCourseActionAsync(int logId, int actorId)
        {
            var actor = await GetAdminWithFaculty(actorId);

            if (!StaffRules.CanDeleteMembers(StaffRules.RoleOf(actor)))
                throw new Exception("Only the dean or the faculty secretary can undo actions");

            var log = await _audit.GetByIdAsync(logId) ?? throw new Exception("Log entry not found");

            if (log.FacultyId != actor.FacultyId)
                throw new Exception("Log entry not found or access denied");
            if (log.IsReverted)
                throw new Exception("This action was already undone");
            if (log.EntityId == null || !ActivityActions.IsReversible(log.EntityType, log.Action))
                throw new Exception("This action can't be undone");

            var summary = log.EntityType == ActivityEntities.Course
                ? await RevertCourseAsync(log, actor)
                : await RevertMembershipAsync(log, actor);

            _audit.Stage(Entry(actor, "Revert", log.EntityType, log.EntityId, $"{summary}: {log.Description}"));
            _audit.MarkReverted(log, actor.Id);
            await _unitOfWork.SaveChangesAsync();

            await ClearCourseCaches(actor.FacultyId, log.EntityId.Value);
            await ClearFacultyCoursesCache(actor.FacultyId);
            return summary;
        }

        private async Task<string> RevertCourseAsync(SystemLog log, Admin actor)
        {
            // _db.Courses مفيهوش فلتر الحذف، فبنقدر نوصل للمحذوفين
            var course = await _db.Courses.Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == log.EntityId!.Value)
                ?? throw new Exception("The course no longer exists");

            if (course.Department == null || course.Department.FacultyId != actor.FacultyId)
                throw new Exception("Course not found or access denied");

            var now = DateTime.UtcNow;
            string summary;

            if (log.Action == ActivityActions.Delete)
            {
                if (!course.IsDeleted) throw new Exception("The course is already restored");
                course.IsDeleted = false;
                course.DeletedAt = null;
                course.DeletedBy = null;
                summary = "Restored deleted course";
            }
            else // Create / Restore
            {
                if (course.IsDeleted) throw new Exception("The course is already deleted");
                course.IsDeleted = true;
                course.DeletedAt = now;
                course.DeletedBy = actor.Id;
                summary = "Undid course creation (course removed)";
            }

            course.UpdatedAt = now;
            course.UpdatedBy = actor.Id;
            await ClearDepartmentCoursesCache(course.DepartmentId);
            return summary;
        }

        private async Task<string> RevertMembershipAsync(SystemLog log, Admin actor)
        {
            var isStudent = log.EntityType == ActivityEntities.CourseStudent;
            var undoAssign = log.Action != ActivityActions.Unassign;
            var ids = ReadIds(undoAssign ? log.NewValues : log.OldValues);
            if (ids.Count == 0) throw new Exception("No recorded members to restore");

            var course = await _db.Courses.Include(c => c.Department)
                .Include(c => c.RegisteredStudents).Include(c => c.Instructors)
                .FirstOrDefaultAsync(c => c.Id == log.EntityId!.Value)
                ?? throw new Exception("The course no longer exists");

            if (course.IsDeleted) throw new Exception("The course is deleted. Restore it first");
            if (course.Department == null || course.Department.FacultyId != actor.FacultyId)
                throw new Exception("Course not found or access denied");

            var changed = 0;

            if (isStudent)
            {
                if (undoAssign)
                {
                    foreach (var s in course.RegisteredStudents.Where(s => ids.Contains(s.Id)).ToList())
                    { course.RegisteredStudents.Remove(s); changed++; }
                }
                else
                {
                    var students = await _db.Students
                        .Where(s => ids.Contains(s.Id) && s.FacultyId == actor.FacultyId && !s.IsDeleted)
                        .ToListAsync();
                    foreach (var s in students)
                        if (!course.RegisteredStudents.Any(x => x.Id == s.Id))
                        { course.RegisteredStudents.Add(s); changed++; }
                }
                foreach (var id in ids) await ClearStudentCoursesCache(id);
            }
            else
            {
                if (undoAssign)
                {
                    foreach (var d in course.Instructors.Where(d => ids.Contains(d.Id)).ToList())
                    { course.Instructors.Remove(d); changed++; }
                }
                else
                {
                    var doctors = await _db.Doctors
                        .Where(d => ids.Contains(d.Id) && d.FacultyId == actor.FacultyId && !d.IsDeleted)
                        .ToListAsync();
                    foreach (var d in doctors)
                        if (!course.Instructors.Any(x => x.Id == d.Id))
                        { course.Instructors.Add(d); changed++; }
                }
                foreach (var id in ids) await ClearDoctorCoursesCache(id);
            }

            return $"Membership restored ({changed} of {ids.Count} applied)";
        }

        private static List<int> ReadIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("Ids", out var arr)
                ? arr.EnumerateArray().Select(e => e.GetInt32()).ToList()
                : new();
        }

        #endregion

        #region Helper Methods

        private static void Validate(object dto)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
                throw new Exception($"Validation failed: {string.Join(", ", results.Select(v => v.ErrorMessage))}");
        }

        private async Task<Admin> GetAdminWithFaculty(int adminId)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId, a => a.Faculty);

            if (admin == null)
                throw new Exception("Admin not found");

            if (!admin.IsActive)
                throw new Exception("Your account is disabled");

            return admin;
        }

        private async Task<Course> GetCourseWithPermissions(int courseId, int facultyId)
        {
            var course = await _unitOfWork.Repository<Course>()
                .GetByIdAsync(courseId,
                    c => c.Department,
                    c => c.Instructors,
                    c => c.RegisteredStudents);

            if (course == null || course.Department.FacultyId != facultyId)
                throw new Exception("Course not found or access denied");

            return course;
        }

        private async Task<Doctor> GetDoctorWithPermissions(int doctorId, int facultyId)
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .GetByIdAsync(doctorId, d => d.Faculty);

            if (doctor == null || doctor.FacultyId != facultyId)
                throw new Exception("Doctor not found or access denied");

            return doctor;
        }

        private async Task<Student> GetStudentWithPermissions(int studentId, int facultyId)
        {
            var student = await _unitOfWork.Repository<Student>()
                .GetByIdAsync(studentId, s => s.Faculty);

            if (student == null || student.FacultyId != facultyId)
                throw new Exception("Student not found or access denied");

            return student;
        }

        private static object CourseSnapshot(Course c) => new { c.Name, c.Description, c.DepartmentId };

        private static AuditEntry Entry(Admin actor, string action, string entityType, int? entityId, string description,
            object? oldValues = null, object? newValues = null) => new()
        {
            ActorId = actor.Id,
            ActorRole = StaffRules.RoleOf(actor),
            FacultyId = actor.FacultyId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues
        };

        #endregion

        #region Cache Management Methods

        private async Task ClearCourseCaches(int facultyId, int courseId)
        {
            await _cacheService.RemoveAsync(CourseKey(facultyId, courseId));
            await _cacheService.RemoveAsync(CourseDoctorsKey(facultyId, courseId));
            await _cacheService.RemoveAsync(CourseStudentsKey(facultyId, courseId));
            await ClearFacultyCoursesCache(facultyId);
        }

        private async Task ClearFacultyCoursesCache(int facultyId) =>
            await _cacheService.RemoveAsync(FacultyCoursesKey(facultyId));

        private async Task ClearDepartmentCoursesCache(int departmentId) =>
            await _cacheService.RemoveAsync($"department_courses_{departmentId}");

        private async Task ClearDoctorCoursesCache(int doctorId) =>
            await _cacheService.RemoveAsync($"doctor_courses_{doctorId}");

        private async Task ClearStudentCoursesCache(int studentId) =>
            await _cacheService.RemoveAsync($"student_courses_{studentId}");

        #endregion

        #region Additional Methods

        public async Task<List<CourseDto>> GetDoctorCoursesAsync(int doctorId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);
            await GetDoctorWithPermissions(doctorId, admin.FacultyId);

            var courses = await _unitOfWork.Repository<Course>()
                .FindAsync(c => c.Instructors.Any(d => d.Id == doctorId),
                    c => c.Department,
                    c => c.Instructors,
                    c => c.RegisteredStudents);

            return _mapper.Map<List<CourseDto>>(courses);
        }

        public async Task<List<CourseDto>> GetStudentCoursesAsync(int studentId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);
            await GetStudentWithPermissions(studentId, admin.FacultyId);

            var courses = await _unitOfWork.Repository<Course>()
                .FindAsync(c => c.RegisteredStudents.Any(s => s.Id == studentId),
                    c => c.Department,
                    c => c.Instructors,
                    c => c.RegisteredStudents);

            return _mapper.Map<List<CourseDto>>(courses);
        }

        #endregion
    }
}
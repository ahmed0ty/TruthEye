using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
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

        private const string COURSES_CACHE_PREFIX = "courses_";
        private const string FACULTY_COURSES_CACHE_PREFIX = "faculty_courses_";
        private const string COURSE_DOCTORS_CACHE_PREFIX = "course_doctors_";
        private const string COURSE_STUDENTS_CACHE_PREFIX = "course_students_";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CourseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CourseService> logger,
            ICacheService cacheService,
            INotificationService notifications)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _notifications = notifications;
        }

        #region Course Management

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto, int adminId)
        {
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(createCourseDto);
                if (!Validator.TryValidateObject(createCourseDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var admin = await GetAdminWithFaculty(adminId);

                var department = await _unitOfWork.Repository<Department>()
                    .GetByIdAsync(createCourseDto.DepartmentId, d => d.Faculty);

                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new Exception("Department does not belong to your faculty");

                var course = _mapper.Map<Course>(createCourseDto);

                await _unitOfWork.Repository<Course>().AddAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearFacultyCoursesCache(admin.FacultyId);
                await ClearDepartmentCoursesCache(createCourseDto.DepartmentId);

                var createdCourse = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(course.Id, c => c.Department);

                _logger.LogInformation("Course created: {CourseId} by Admin {AdminId}", course.Id, adminId);

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
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(updateCourseDto);
                if (!Validator.TryValidateObject(updateCourseDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var admin = await GetAdminWithFaculty(adminId);

                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(courseId, c => c.Department);

                if (course == null || course.Department.FacultyId != admin.FacultyId)
                    throw new Exception("Course not found or access denied");

                if (updateCourseDto.DepartmentId != course.DepartmentId)
                {
                    var newDepartment = await _unitOfWork.Repository<Department>()
                        .GetByIdAsync(updateCourseDto.DepartmentId);

                    if (newDepartment == null || newDepartment.FacultyId != admin.FacultyId)
                        throw new Exception("New department does not belong to your faculty");
                }

                course.Name = updateCourseDto.Name;
                course.Description = updateCourseDto.Description;
                course.DepartmentId = updateCourseDto.DepartmentId;
                course.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCache(courseId);
                await ClearFacultyCoursesCache(admin.FacultyId);
                await ClearDepartmentCoursesCache(course.DepartmentId);
                if (updateCourseDto.DepartmentId != course.DepartmentId)
                {
                    await ClearDepartmentCoursesCache(updateCourseDto.DepartmentId);
                }

                _logger.LogInformation("Course updated: {CourseId} by Admin {AdminId}", courseId, adminId);

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

                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(courseId, c => c.Department);

                if (course == null || course.Department.FacultyId != admin.FacultyId)
                    throw new Exception("Course not found or access denied");

                await _unitOfWork.Repository<Course>().DeleteAsync(course);
                await _unitOfWork.SaveChangesAsync();
                await ClearCourseCache(courseId);
                await ClearFacultyCoursesCache(admin.FacultyId);
                await ClearDepartmentCoursesCache(course.DepartmentId);

                _logger.LogInformation("Course deleted: {CourseId} by Admin {AdminId}", courseId, adminId);

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
            var cacheKey = $"{COURSES_CACHE_PREFIX}{courseId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var admin = await GetAdminWithFaculty(adminId);

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
            var cacheKey = $"{FACULTY_COURSES_CACHE_PREFIX}{admin.FacultyId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
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
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await _notifications.SendNotificationAsync(doctorId, $"You were assigned to course: {course.Name}", "Course");

                await ClearCourseCache(courseId);
                await ClearCourseDoctorsCache(courseId);
                await ClearDoctorCoursesCache(doctorId);

                _logger.LogInformation("Doctor {DoctorId} assigned to course {CourseId} by Admin {AdminId}",
                    doctorId, courseId, adminId);

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
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCache(courseId);
                await ClearCourseDoctorsCache(courseId);
                await ClearDoctorCoursesCache(doctorId);

                _logger.LogInformation("Doctor {DoctorId} removed from course {CourseId} by Admin {AdminId}",
                    doctorId, courseId, adminId);

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
            var cacheKey = $"{COURSE_DOCTORS_CACHE_PREFIX}{courseId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var admin = await GetAdminWithFaculty(adminId);
                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);

                _logger.LogInformation("Retrieved {Count} doctors from database for course {CourseId}",
                    course.Instructors.Count, courseId);

                return _mapper.Map<List<DoctorDto>>(course.Instructors);
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
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await _notifications.SendNotificationAsync(studentId, $"You were added to course: {course.Name}", "Course");

                await ClearCourseCache(courseId);
                await ClearCourseStudentsCache(courseId);
                await ClearStudentCoursesCache(studentId);

                _logger.LogInformation("Student {StudentId} assigned to course {CourseId} by Admin {AdminId}",
                    studentId, courseId, adminId);

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
                await _unitOfWork.Repository<Course>().UpdateAsync(course);
                await _unitOfWork.SaveChangesAsync();

                await ClearCourseCache(courseId);
                await ClearCourseStudentsCache(courseId);
                await ClearStudentCoursesCache(studentId);

                _logger.LogInformation("Student {StudentId} removed from course {CourseId} by Admin {AdminId}",
                    studentId, courseId, adminId);

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
            var cacheKey = $"{COURSE_STUDENTS_CACHE_PREFIX}{courseId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var admin = await GetAdminWithFaculty(adminId);
                var course = await GetCourseWithPermissions(courseId, admin.FacultyId);

                _logger.LogInformation("Retrieved {Count} students from database for course {CourseId}",
                    course.RegisteredStudents.Count, courseId);

                return _mapper.Map<List<StudentDto>>(course.RegisteredStudents);
            }, _cacheExpiration);
        }

        #endregion

        #region Helper Methods

        private async Task<Admin> GetAdminWithFaculty(int adminId)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId, a => a.Faculty);

            if (admin == null)
                throw new Exception("Admin not found");

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

        #endregion

        #region Cache Management Methods

        private async Task ClearCourseCache(int courseId)
        {
            await _cacheService.RemoveAsync($"{COURSES_CACHE_PREFIX}{courseId}");
        }

        private async Task ClearFacultyCoursesCache(int facultyId)
        {
            await _cacheService.RemoveAsync($"{FACULTY_COURSES_CACHE_PREFIX}{facultyId}");
        }

        private async Task ClearCourseDoctorsCache(int courseId)
        {
            await _cacheService.RemoveAsync($"{COURSE_DOCTORS_CACHE_PREFIX}{courseId}");
        }

        private async Task ClearCourseStudentsCache(int courseId)
        {
            await _cacheService.RemoveAsync($"{COURSE_STUDENTS_CACHE_PREFIX}{courseId}");
        }

        private async Task ClearDepartmentCoursesCache(int departmentId)
        {
            await _cacheService.RemoveAsync($"department_courses_{departmentId}");
        }

        private async Task ClearDoctorCoursesCache(int doctorId)
        {
            await _cacheService.RemoveAsync($"doctor_courses_{doctorId}");
        }

        private async Task ClearStudentCoursesCache(int studentId)
        {
            await _cacheService.RemoveAsync($"student_courses_{studentId}");
        }

        #endregion

        #region Additional Methods

        public async Task<List<CourseDto>> GetDoctorCoursesAsync(int doctorId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);
            var doctor = await GetDoctorWithPermissions(doctorId, admin.FacultyId);

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
            var student = await GetStudentWithPermissions(studentId, admin.FacultyId);

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
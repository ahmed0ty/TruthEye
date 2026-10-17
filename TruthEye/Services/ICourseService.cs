using TruthEye.DTOs;

namespace TruthEye.Services
{
    public interface ICourseService
    {
        Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto, int adminId);
        Task<CourseDto> UpdateCourseAsync(int courseId, UpdateCourseDto updateCourseDto, int adminId);
        Task<bool> DeleteCourseAsync(int courseId, int adminId);
        Task<CourseDto> GetCourseByIdAsync(int courseId, int adminId);
        Task<List<CourseDto>> GetFacultyCoursesAsync(int adminId);

        Task<bool> AssignDoctorToCourseAsync(int courseId, int doctorId, int adminId);
        Task<bool> RemoveDoctorFromCourseAsync(int courseId, int doctorId, int adminId);
        Task<List<DoctorDto>> GetCourseDoctorsAsync(int courseId, int adminId);

        Task<bool> AssignStudentToCourseAsync(int courseId, int studentId, int adminId);
        Task<bool> RemoveStudentFromCourseAsync(int courseId, int studentId, int adminId);
        Task<List<StudentDto>> GetCourseStudentsAsync(int courseId, int adminId);

        Task<List<CourseDto>> GetDoctorCoursesAsync(int doctorId, int adminId);

        Task<List<CourseDto>> GetStudentCoursesAsync(int studentId, int adminId);
    }
}

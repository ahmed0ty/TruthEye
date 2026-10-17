using TruthEye.DTOs;

namespace TruthEye.Services
{
    public interface IAuthService
    {
        Task<TokenPair> LoginAsync(LoginDto loginDto);
        Task<TokenPair> RefreshAsync(string refreshToken);
        Task RevokeRefreshAsync(string refreshToken);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<AdminDto> RegisterSuperAdminAsync(RegisterUserDto registerDto);
        Task<AdminDto> RegisterFacultyAdminAsync(RegisterAdminDto registerDto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);

        // Student Management (Admin only)
        Task<StudentDto> RegisterStudentAsync(RegisterStudentDto registerDto, int adminId);
        Task<List<StudentDto>> GetFacultyStudentsAsync(int adminId);
        Task<StudentDto> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, int adminId);
        Task<bool> DeleteStudentAsync(int studentId, int adminId);

        // Doctor Management (Admin only)
        Task<DoctorDto> RegisterDoctorAsync(RegisterDoctorDto registerDto, int adminId);
        Task<List<DoctorDto>> GetFacultyDoctorsAsync(int adminId);
        Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int adminId);
        Task<bool> DeleteDoctorAsync(int doctorId, int adminId);

        // Proctor Management (Admin only)
        Task<ProctorDto> RegisterProctorAsync(RegisterProctorDto registerDto, int adminId);
        Task<List<ProctorDto>> GetFacultyProctorsAsync(int adminId);
        Task<ProctorDto> UpdateProctorAsync(int proctorId, UpdateProctorDto updateDto, int adminId);
        Task<bool> DeleteProctorAsync(int proctorId, int adminId);

        // Support Management (SuperAdmin only)
        Task<SupportDto> RegisterSupportAsync(RegisterSupportDto registerDto);
        Task<List<SupportDto>> GetAllSupportsAsync();
        Task<SupportDto> UpdateSupportAsync(int supportId, UpdateSupportDto updateDto);
        Task<bool> DeleteSupportAsync(int supportId);

        // Admin Management (SuperAdmin only)
        Task<List<AdminDto>> GetAllAdminsAsync();
        Task<AdminDto> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto);
        Task<bool> DeleteAdminAsync(int adminId);
        Task<bool> SuperAdminExistsAsync();
    }
    public sealed record TokenPair(string AccessToken, string RefreshToken);
}

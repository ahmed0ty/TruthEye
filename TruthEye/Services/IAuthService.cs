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
        Task<AdminDto> RegisterFacultyAdminAsync(RegisterAdminDto registerDto, int actorId = 0);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);

        // Students (Dean / Secretary / Technical)
        Task<StudentDto> RegisterStudentAsync(RegisterStudentDto registerDto, int adminId);
        Task<List<StudentDto>> GetFacultyStudentsAsync(int adminId, bool includeDisabled = false);
        Task<StudentDto> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, int adminId);
        Task<bool> DisableStudentAsync(int studentId, int adminId);
        Task<bool> EnableStudentAsync(int studentId, int adminId);
        Task<bool> DeleteStudentAsync(int studentId, int adminId);   // حذف (IsDeleted): عميد وأمين بس

        // Doctors
        Task<DoctorDto> RegisterDoctorAsync(RegisterDoctorDto registerDto, int adminId);
        Task<List<DoctorDto>> GetFacultyDoctorsAsync(int adminId, bool includeDisabled = false);
        Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int adminId);
        Task<bool> DisableDoctorAsync(int doctorId, int adminId);
        Task<bool> EnableDoctorAsync(int doctorId, int adminId);
        Task<bool> DeleteDoctorAsync(int doctorId, int adminId);

        // Proctors
        Task<ProctorDto> RegisterProctorAsync(RegisterProctorDto registerDto, int adminId);
        Task<List<ProctorDto>> GetFacultyProctorsAsync(int adminId, bool includeDisabled = false);
        Task<ProctorDto> UpdateProctorAsync(int proctorId, UpdateProctorDto updateDto, int adminId);
        Task<bool> DisableProctorAsync(int proctorId, int adminId);
        Task<bool> EnableProctorAsync(int proctorId, int adminId);
        Task<bool> DeleteProctorAsync(int proctorId, int adminId);

        // Faculty staff: أمين الكلية والتيم الفني
        Task<StaffDto> RegisterStaffAsync(RegisterStaffDto dto, int actorId);
        Task<List<StaffDto>> GetStaffAsync(int actorId, bool includeDisabled = true);
        Task<StaffDto> UpdateStaffAsync(int staffId, UpdateStaffDto dto, int actorId);
        Task<bool> DisableStaffAsync(int staffId, int actorId);
        Task<bool> EnableStaffAsync(int staffId, int actorId);
        Task<bool> DeleteStaffAsync(int staffId, int actorId);

        // التراجع عن عملية على طالب أو دكتور أو مراقب أو عضو تيم (الكورسات في الجزء 3)
        Task<string> RevertMemberActionAsync(int logId, int actorId);

        // Support (SuperAdmin only)
        Task<SupportDto> RegisterSupportAsync(RegisterSupportDto registerDto);
        Task<List<SupportDto>> GetAllSupportsAsync();
        Task<SupportDto> UpdateSupportAsync(int supportId, UpdateSupportDto updateDto);
        Task<bool> DeleteSupportAsync(int supportId);

        // Deans (SuperAdmin only)
        Task<List<AdminDto>> GetAllAdminsAsync();
        Task<AdminDto> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto, int actorId = 0);
        Task<bool> DeleteAdminAsync(int adminId, int actorId = 0);
        Task<bool> SuperAdminExistsAsync();
    }

    public sealed record TokenPair(string AccessToken, string RefreshToken);
}
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.UnitOfWorkk;
using System.Security.Cryptography;
using TruthEye.Data;

namespace TruthEye.Services
{
        public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();

        private const int MaxFailures = 5;
        private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _db = db;
        }

        #region Authentication Methods

        public async Task<TokenPair> LoginAsync(LoginDto loginDto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(loginDto);
            if (!Validator.TryValidateObject(loginDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            if (string.IsNullOrEmpty(loginDto.Email) && string.IsNullOrEmpty(loginDto.NationalId))
            {
                throw new Exception("Either email or national ID must be provided");
            }

            // Brute-force protection: the lockout counter lives in the database
            var key = "login:" + (loginDto.Email ?? loginDto.NationalId ?? "").Trim().ToLowerInvariant();
            var lockout = await _db.LoginLockouts.FirstOrDefaultAsync(l => l.Key == key);

            if (lockout?.LockedUntil is { } until && until > DateTime.UtcNow)
            {
                var mins = (int)Math.Ceiling((until - DateTime.UtcNow).TotalMinutes);
                throw new Exception($"Too many failed attempts. Try again in {mins} minute(s).");
            }

            User user = null;

            if (!string.IsNullOrEmpty(loginDto.Email))
            {
                var users = await _unitOfWork.Repository<User>()
                    .FindAsync(u => u.Email == loginDto.Email, u => u.Role);
                user = users.FirstOrDefault();
            }

            if (user == null && !string.IsNullOrEmpty(loginDto.NationalId))
            {
                var users = await _unitOfWork.Repository<User>()
                    .FindAsync(u => u.NationalId == loginDto.NationalId, u => u.Role);
                user = users.FirstOrDefault();
            }

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                await RegisterFailureAsync(key, lockout);
                throw new Exception("Invalid credentials");
            }

            if (lockout != null)
                await ClearFailuresAsync(lockout);

            if (!user.IsActive)
                throw new Exception("Account is disabled");

            user.LastLogin = DateTime.UtcNow;
            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return new TokenPair(GenerateJwtToken(user), await IssueRefreshAsync(user.Id));
        }

        private async Task RegisterFailureAsync(string key, LoginLockout? lockout)
        {
            var now = DateTime.UtcNow;

            if (lockout == null)
            {
                lockout = new LoginLockout { Key = key };
                _db.LoginLockouts.Add(lockout);
            }
            // A failure after an expired lock starts a fresh count
            else if (lockout.LockedUntil != null && lockout.LockedUntil <= now)
            {
                lockout.LockedUntil = null;
                lockout.Failures = 0;
            }

            lockout.Failures++;
            lockout.UpdatedAt = now;

            if (lockout.Failures >= MaxFailures)
            {
                lockout.LockedUntil = now + LockFor;
                lockout.Failures = 0;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two failed attempts created the same key at once: the unique index wins, the counter just misses one tick
            }
        }

        private async Task ClearFailuresAsync(LoginLockout lockout)
        {
            _db.LoginLockouts.Remove(lockout);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> SuperAdminExistsAsync()
        {
            var all = await _unitOfWork.Repository<SuperAdmin>().GetAllAsync();
            return all.Any();
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, u => u.Role);

            if (user == null)
                throw new Exception("User not found");

            return user switch
            {
                Student s => await MapStudentWithAllData(s),
                Doctor d => await MapDoctorWithAllData(d),
                Admin a => await MapAdminWithAllData(a),
                Proctor p => await MapProctorWithAllData(p),
                Support s => await MapSupportWithAllData(s),
                SuperAdmin sa => MapSuperAdminWithAllData(sa),
                _ => _mapper.Map<UserDto>(user)
            };
        }

        public async Task<AdminDto> RegisterSuperAdminAsync(RegisterUserDto registerDto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(registerDto);
            if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            if (await SuperAdminExistsAsync())
                throw new Exception("Setup already completed. A system admin already exists.");

            var existingUser = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

            if (existingUser.Any())
                throw new Exception("User with this email or national ID already exists");

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "SuperAdmin"))
                .FirstOrDefault() ?? throw new Exception("Role 'SuperAdmin' not found");

            var superAdmin = new SuperAdmin
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                NationalId = registerDto.NationalId,
                PhoneNumber = registerDto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                TotalActionsPerformed = 0,
                RoleId = role.Id,
                ProfileImage = "default.jpg"
            };

            await _unitOfWork.Repository<SuperAdmin>().AddAsync(superAdmin);
            await _unitOfWork.SaveChangesAsync();

            var adminDto = _mapper.Map<AdminDto>(superAdmin);
            adminDto.Role = "SuperAdmin";
            adminDto.UserType = "SuperAdmin";

            return adminDto;
        }

        public async Task<AdminDto> RegisterFacultyAdminAsync(RegisterAdminDto registerDto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(registerDto);
            if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var existingUser = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

            if (existingUser.Any())
                throw new Exception("User with this email or national ID already exists");

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Admin"))
                .FirstOrDefault() ?? throw new Exception("Role 'Admin' not found");

            var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(registerDto.FacultyId);
            if (faculty == null)
                throw new Exception("Faculty not found");

            var admin = new Admin
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                NationalId = registerDto.NationalId,
                PhoneNumber = registerDto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                UniversityId = faculty.UniversityId,
                FacultyId = registerDto.FacultyId,
                RoleId = role.Id,
                ProfileImage = "default.jpg"
            };

            await _unitOfWork.Repository<Admin>().AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            var createdAdmin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(admin.Id, a => a.University, a => a.Faculty, a => a.Role);

            var adminDto = _mapper.Map<AdminDto>(createdAdmin);
            adminDto.Role = "Admin";
            adminDto.UserType = "Admin";

            return adminDto;
        }

        #endregion

        #region Student Management (Admin only)

        public async Task<StudentDto> RegisterStudentAsync(RegisterStudentDto registerDto, int adminId)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(registerDto);
            if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var admin = await GetAdminWithFaculty(adminId);

            var existingUser = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

            if (existingUser.Any())
                throw new Exception("User with this email or national ID already exists");

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Student"))
                .FirstOrDefault() ?? throw new Exception("Student role not found");
            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId);
            if (department == null)
                throw new Exception("Department not found");

            if (department.FacultyId != admin.FacultyId)
                throw new Exception("Department does not belong to your faculty");
            var student = _mapper.Map<Student>(registerDto);
            student.PasswordHash = HashPassword(registerDto.Password);
            student.RoleId = role.Id;
            student.FacultyId = admin.FacultyId;
            student.UniversityId = admin.UniversityId;
            student.ProfileImage = "default.jpg";
            student.CreatedAt = DateTime.UtcNow;
            student.IsActive = true;
            await _unitOfWork.Repository<Student>().AddAsync(student);
            await _unitOfWork.SaveChangesAsync();

            var createdStudent = await _unitOfWork.Repository<Student>()
                .GetByIdAsync(student.Id,
                    s => s.University,
                    s => s.Faculty,
                    s => s.Department,
                    s => s.Role);

            return _mapper.Map<StudentDto>(createdStudent);
        }

        public async Task<List<StudentDto>> GetFacultyStudentsAsync(int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var students = await _unitOfWork.Repository<Student>()
                .FindAsync(s => s.FacultyId == admin.FacultyId && s.IsActive,
                    s => s.University,
                    s => s.Faculty,
                    s => s.Department,
                    s => s.Role);

            return _mapper.Map<List<StudentDto>>(students);
        }

        public async Task<StudentDto> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, int adminId)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(updateDto);
            if (!Validator.TryValidateObject(updateDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var admin = await GetAdminWithFaculty(adminId);

            var student = await _unitOfWork.Repository<Student>()
                .GetByIdAsync(studentId,
                    s => s.University,
                    s => s.Faculty,
                    s => s.Department,
                    s => s.Role);

            if (student == null || student.FacultyId != admin.FacultyId)
                throw new Exception("Student not found or access denied");

            if (updateDto.DepartmentId != student.DepartmentId)
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(updateDto.DepartmentId);
                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new Exception("Department does not belong to your faculty");
            }

            student.FirstName = updateDto.FirstName;
            student.LastName = updateDto.LastName;
            student.PhoneNumber = updateDto.PhoneNumber;
            student.DepartmentId = updateDto.DepartmentId;
            student.Year = updateDto.Year;
            student.IsActive = updateDto.IsActive;
            student.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Student>().UpdateAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StudentDto>(student);
        }

        public async Task<bool> DeleteStudentAsync(int studentId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var student = await _unitOfWork.Repository<Student>()
                .GetByIdAsync(studentId);

            if (student == null || student.FacultyId != admin.FacultyId)
                throw new Exception("Student not found or access denied");

            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<Student>().UpdateAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Doctor Management (Admin only)

        public async Task<DoctorDto> RegisterDoctorAsync(RegisterDoctorDto registerDto, int adminId)
        {
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(registerDto);
                if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var admin = await GetAdminWithFaculty(adminId);

                var existingUser = await _unitOfWork.Repository<User>()
                    .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

                if (existingUser.Any())
                    throw new Exception("User with this email or national ID already exists");

                var role = (await _unitOfWork.Repository<Role>()
                    .FindAsync(r => r.Name == "Doctor"))
                    .FirstOrDefault() ?? throw new Exception("Doctor role not found");

                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId);
                if (department == null)
                    throw new Exception("Department not found");

                if (department.FacultyId != admin.FacultyId)
                    throw new Exception("Department does not belong to your faculty");

                var doctor = _mapper.Map<Doctor>(registerDto);
                doctor.PasswordHash = HashPassword(registerDto.Password);
                doctor.RoleId = role.Id;
                doctor.FacultyId = admin.FacultyId;
                doctor.UniversityId = admin.UniversityId;
                doctor.ProfileImage = "default.jpg";
                doctor.CreatedAt = DateTime.UtcNow;
                doctor.IsActive = true;
                await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
                await _unitOfWork.SaveChangesAsync();

                var createdDoctor = await _unitOfWork.Repository<Doctor>()
                    .GetByIdAsync(doctor.Id,
                        d => d.University,
                        d => d.Faculty,
                        d => d.Department,
                        d => d.Role);

                return _mapper.Map<DoctorDto>(createdDoctor);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new Exception($"Database error: {innerMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error registering doctor: {ex.Message}");
            }
        }

        public async Task<List<DoctorDto>> GetFacultyDoctorsAsync(int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var doctors = await _unitOfWork.Repository<Doctor>()
                .FindAsync(d => d.FacultyId == admin.FacultyId && d.IsActive,
                    d => d.University,
                    d => d.Faculty,
                    d => d.Department,
                    d => d.Role);

            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        public async Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int adminId)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(updateDto);
            if (!Validator.TryValidateObject(updateDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var admin = await GetAdminWithFaculty(adminId);

            var doctor = await _unitOfWork.Repository<Doctor>()
                .GetByIdAsync(doctorId,
                    d => d.University,
                    d => d.Faculty,
                    d => d.Department,
                    d => d.Role);

            if (doctor == null || doctor.FacultyId != admin.FacultyId)
                throw new Exception("Doctor not found or access denied");

            if (updateDto.DepartmentId != doctor.DepartmentId)
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(updateDto.DepartmentId);
                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new Exception("Department does not belong to your faculty");
            }

            doctor.FirstName = updateDto.FirstName;
            doctor.LastName = updateDto.LastName;
            doctor.PhoneNumber = updateDto.PhoneNumber;
            doctor.DepartmentId = updateDto.DepartmentId;
            doctor.Specialization = updateDto.Specialization;
            doctor.AcademicDegree = updateDto.AcademicDegree;
            doctor.YearsOfExperience = updateDto.YearsOfExperience;
            doctor.IsActive = updateDto.IsActive;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DoctorDto>(doctor);
        }

        public async Task<bool> DeleteDoctorAsync(int doctorId, int adminId)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var doctor = await _unitOfWork.Repository<Doctor>()
                .GetByIdAsync(doctorId);

            if (doctor == null || doctor.FacultyId != admin.FacultyId)
                throw new Exception("Doctor not found or access denied");

            doctor.IsActive = false;
            doctor.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Change Password Methods

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(changePasswordDto);
                if (!Validator.TryValidateObject(changePasswordDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null)
                    throw new Exception("User not found");

                if (!user.IsActive)
                    throw new Exception("Account is disabled");

                if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                    throw new Exception("Current password is incorrect");

                user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Changing the password signs the user out of all other devices
                var open = _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null);
                await open.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error changing password: {ex.Message}");
            }
        }

        #endregion

        #region Proctor Management (Admin only)

        public async Task<ProctorDto> RegisterProctorAsync(RegisterProctorDto registerDto, int adminId)
        {
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(registerDto);
                if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var existingUser = await _unitOfWork.Repository<User>()
                    .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

                if (existingUser.Any())
                    throw new Exception("User with this email or national ID already exists");

                var role = (await _unitOfWork.Repository<Role>()
                    .FindAsync(r => r.Name == "Proctor"))
                    .FirstOrDefault() ?? throw new Exception("Proctor role not found");

                var proctor = _mapper.Map<Proctor>(registerDto);
                proctor.PasswordHash = HashPassword(registerDto.Password);
                proctor.RoleId = role.Id;
                proctor.ProfileImage = "default.jpg";
                proctor.CreatedAt = DateTime.UtcNow;
                proctor.IsActive = true;
                proctor.IsOnDuty = false;
                proctor.ActiveSessionsCount = 0;
                proctor.AverageResponseTime = 0.0;

                await _unitOfWork.Repository<Proctor>().AddAsync(proctor);
                await _unitOfWork.SaveChangesAsync();

                var createdProctor = await _unitOfWork.Repository<Proctor>()
                    .GetByIdAsync(proctor.Id, p => p.Role);

                return _mapper.Map<ProctorDto>(createdProctor);
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException;
                var fullError = innerException?.Message ?? dbEx.Message;
                throw new Exception($"Database update error: {fullError}. StackTrace: {innerException?.StackTrace}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error registering proctor: {ex.Message}. Inner: {ex.InnerException?.Message}");
            }
        }

        public async Task<List<ProctorDto>> GetFacultyProctorsAsync(int adminId)
        {
            var proctors = await _unitOfWork.Repository<Proctor>()
                .FindAsync(p => p.IsActive, p => p.Role);

            return _mapper.Map<List<ProctorDto>>(proctors);
        }

        public async Task<ProctorDto> UpdateProctorAsync(int proctorId, UpdateProctorDto updateDto, int adminId)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(updateDto);
            if (!Validator.TryValidateObject(updateDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var proctor = await _unitOfWork.Repository<Proctor>()
                .GetByIdAsync(proctorId, p => p.Role);

            if (proctor == null)
                throw new Exception("Proctor not found");

            _mapper.Map(updateDto, proctor);
            await _unitOfWork.Repository<Proctor>().UpdateAsync(proctor);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProctorDto>(proctor);
        }

        public async Task<bool> DeleteProctorAsync(int proctorId, int adminId)
        {
            var proctor = await _unitOfWork.Repository<Proctor>()
                .GetByIdAsync(proctorId);

            if (proctor == null)
                throw new Exception("Proctor not found");

            proctor.IsActive = false;
            await _unitOfWork.Repository<Proctor>().UpdateAsync(proctor);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Support Management (SuperAdmin only)

        public async Task<SupportDto> RegisterSupportAsync(RegisterSupportDto registerDto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(registerDto);
            if (!Validator.TryValidateObject(registerDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var existingUser = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Email == registerDto.Email || u.NationalId == registerDto.NationalId);

            if (existingUser.Any())
                throw new Exception("User with this email or national ID already exists");

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Support"))
                .FirstOrDefault() ?? throw new Exception("Support role not found");

            var support = _mapper.Map<Support>(registerDto);
            support.PasswordHash = HashPassword(registerDto.Password);
            support.RoleId = role.Id;
            support.ProfileImage = "default.jpg";
            support.CreatedAt = DateTime.UtcNow;
            support.IsActive = true;
            await _unitOfWork.Repository<Support>().AddAsync(support);
            await _unitOfWork.SaveChangesAsync();

            var createdSupport = await _unitOfWork.Repository<Support>()
                .GetByIdAsync(support.Id, s => s.Role);

            return _mapper.Map<SupportDto>(createdSupport);
        }

        public async Task<List<SupportDto>> GetAllSupportsAsync()
        {
            var supports = await _unitOfWork.Repository<Support>()
                .FindAsync(s => s.IsActive, s => s.Role);

            return _mapper.Map<List<SupportDto>>(supports);
        }

        public async Task<SupportDto> UpdateSupportAsync(int supportId, UpdateSupportDto updateDto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(updateDto);
            if (!Validator.TryValidateObject(updateDto, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }

            var support = await _unitOfWork.Repository<Support>()
                .GetByIdAsync(supportId, s => s.Role);

            if (support == null)
                throw new Exception("Support not found");

            _mapper.Map(updateDto, support);
            await _unitOfWork.Repository<Support>().UpdateAsync(support);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportDto>(support);
        }

        public async Task<bool> DeleteSupportAsync(int supportId)
        {
            var support = await _unitOfWork.Repository<Support>()
                .GetByIdAsync(supportId);

            if (support == null)
                throw new Exception("Support not found");

            support.IsActive = false;
            await _unitOfWork.Repository<Support>().UpdateAsync(support);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Admin Management (SuperAdmin only)

        public async Task<List<AdminDto>> GetAllAdminsAsync()
        {
            var admins = await _unitOfWork.Repository<Admin>()
                .FindAsync(a => a.IsActive,
                    a => a.University,
                    a => a.Faculty,
                    a => a.Role);

            return _mapper.Map<List<AdminDto>>(admins);
        }

        public async Task<AdminDto> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto)
        {
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(updateDto);
                if (!Validator.TryValidateObject(updateDto, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new Exception($"Validation failed: {errors}");
                }

                var university = await _unitOfWork.Repository<University>().GetByIdAsync(updateDto.UniversityId);
                if (university == null)
                    throw new Exception("University not found");

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(updateDto.FacultyId);
                if (faculty == null)
                    throw new Exception("Faculty not found");

                if (faculty.UniversityId != university.Id)
                    throw new Exception("Faculty does not belong to the specified university");

                var admin = await _unitOfWork.Repository<Admin>()
                    .GetByIdAsync(adminId,
                        a => a.University,
                        a => a.Faculty,
                        a => a.Role);

                if (admin == null)
                    throw new Exception("Admin not found");

                admin.FirstName = updateDto.FirstName;
                admin.LastName = updateDto.LastName;
                admin.PhoneNumber = updateDto.PhoneNumber;
                admin.UniversityId = updateDto.UniversityId;
                admin.FacultyId = updateDto.FacultyId;
                admin.IsActive = updateDto.IsActive;
                admin.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<Admin>().UpdateAsync(admin);
                await _unitOfWork.SaveChangesAsync();

                var updatedAdmin = await _unitOfWork.Repository<Admin>()
                    .GetByIdAsync(adminId,
                        a => a.University,
                        a => a.Faculty,
                        a => a.Role);

                return _mapper.Map<AdminDto>(updatedAdmin);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new Exception($"Database error while updating admin: {innerMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating admin: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId);

            if (admin == null)
                throw new Exception("Admin not found");

            admin.IsActive = false;
            await _unitOfWork.Repository<Admin>().UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return true;
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

        private async Task<UserDto> MapStudentWithAllData(Student student)
        {
            if (student.University == null && student.UniversityId > 0)
                student.University = await _unitOfWork.Repository<University>().GetByIdAsync(student.UniversityId);

            if (student.Faculty == null && student.FacultyId > 0)
                student.Faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(student.FacultyId);

            if (student.Department == null && student.DepartmentId > 0)
                student.Department = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);

            return _mapper.Map<StudentDto>(student);
        }

        private async Task<UserDto> MapDoctorWithAllData(Doctor doctor)
        {
            if (doctor.University == null && doctor.UniversityId > 0)
                doctor.University = await _unitOfWork.Repository<University>().GetByIdAsync(doctor.UniversityId);

            if (doctor.Faculty == null && doctor.FacultyId > 0)
                doctor.Faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(doctor.FacultyId);

            if (doctor.Department == null && doctor.DepartmentId > 0)
                doctor.Department = await _unitOfWork.Repository<Department>().GetByIdAsync(doctor.DepartmentId);

            return _mapper.Map<DoctorDto>(doctor);
        }

        private async Task<UserDto> MapAdminWithAllData(Admin admin)
        {
            if (admin.University == null && admin.UniversityId > 0)
                admin.University = await _unitOfWork.Repository<University>().GetByIdAsync(admin.UniversityId);

            if (admin.Faculty == null && admin.FacultyId > 0)
                admin.Faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(admin.FacultyId);

            return _mapper.Map<AdminDto>(admin);
        }

        private async Task<UserDto> MapProctorWithAllData(Proctor proctor)
        {
            return _mapper.Map<ProctorDto>(proctor);
        }

        private async Task<UserDto> MapSupportWithAllData(Support support)
        {
            return _mapper.Map<SupportDto>(support);
        }

        private UserDto MapSuperAdminWithAllData(SuperAdmin superAdmin)
        {
            var dto = _mapper.Map<AdminDto>(superAdmin);
            dto.Role = "SuperAdmin";
            dto.UserType = "SuperAdmin";
            return dto;
        }

        private string HashPassword(string password)
        {
            var user = new User();
            return _passwordHasher.HashPassword(user, password);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var user = new User();
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
            return result == PasswordVerificationResult.Success;
        }

        private static string HashToken(string raw) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private async Task<string> IssueRefreshAsync(int userId)
        {
            var now = DateTime.UtcNow;

            // clean up this user's dead tokens
            var dead = _db.RefreshTokens.Where(t => t.UserId == userId &&
                (t.ExpiresAt < now || (t.RevokedAt != null && t.RevokedAt < now.AddDays(-1))));
            _db.RefreshTokens.RemoveRange(dead);

            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                CreatedAt = now,
                ExpiresAt = now.AddDays(7)
            });
            await _db.SaveChangesAsync();
            return raw;
        }

        public async Task<TokenPair> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Invalid refresh token");

            var hash = HashToken(refreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored == null || stored.RevokedAt != null || stored.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid refresh token");

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(stored.UserId, u => u.Role);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled");

            stored.RevokedAt = DateTime.UtcNow;   // rotation: the old one dies, saved by IssueRefreshAsync
            return new TokenPair(GenerateJwtToken(user), await IssueRefreshAsync(user.Id));
        }

        public async Task RevokeRefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;
            var hash = HashToken(refreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored != null && stored.RevokedAt == null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        private string GenerateJwtToken(User user)
        {
            var userType = user switch
            {
                Student => "Student",
                Doctor => "Doctor",
                SuperAdmin => "SuperAdmin",
                Admin => "Admin",
                Proctor => "Proctor",
                Support => "Support",
                _ => "User"
            };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, userType),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int?>("Jwt:AccessMinutes") ?? 15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion
    }
}
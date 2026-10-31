using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TruthEye.Data;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly ISystemLogService _audit;
        private readonly PasswordHasher<User> _passwordHasher = new();

        private const int MaxFailures = 5;
        private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);
        private const string RevertAction = "Revert";

        // الحقول اللي بتتسجّل في السجل وبيتراجع عنها
        private static readonly Dictionary<string, string[]> Tracked = new()
        {
            [ActivityEntities.Student] = new[] { "FirstName", "LastName", "PhoneNumber", "DepartmentId", "Year", "IsActive" },
            [ActivityEntities.Doctor] = new[] { "FirstName", "LastName", "PhoneNumber", "DepartmentId", "Specialization", "AcademicDegree", "YearsOfExperience", "IsActive" },
            [ActivityEntities.Proctor] = new[] { "FirstName", "LastName", "PhoneNumber", "Department", "IsActive" },
            [ActivityEntities.Staff] = new[] { "FirstName", "LastName", "PhoneNumber", "IsActive" },
        };

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, AppDbContext db, ISystemLogService audit)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _db = db;
            _audit = audit;
        }

        #region Authentication Methods

        public async Task<TokenPair> LoginAsync(LoginDto loginDto)
        {
            Validate(loginDto);

            if (string.IsNullOrEmpty(loginDto.Email) && string.IsNullOrEmpty(loginDto.NationalId))
                throw new Exception("Either email or national ID must be provided");

            // Brute-force protection: the lockout counter lives in the database
            var key = "login:" + (loginDto.Email ?? loginDto.NationalId ?? "").Trim().ToLowerInvariant();
            var lockout = await _db.LoginLockouts.FirstOrDefaultAsync(l => l.Key == key);

            if (lockout?.LockedUntil is { } until && until > DateTime.UtcNow)
            {
                var mins = (int)Math.Ceiling((until - DateTime.UtcNow).TotalMinutes);
                throw new Exception($"Too many failed attempts. Try again in {mins} minute(s).");
            }

            User? user = null;

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
                Proctor p => MapProctorWithAllData(p),
                Support sp => MapSupportWithAllData(sp),
                SuperAdmin sa => MapSuperAdminWithAllData(sa),
                _ => _mapper.Map<UserDto>(user)
            };
        }

        public async Task<AdminDto> RegisterSuperAdminAsync(RegisterUserDto registerDto)
        {
            Validate(registerDto);

            if (await SuperAdminExistsAsync())
                throw new Exception("Setup already completed. A system admin already exists.");

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

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

        /// <summary>رئيس الجامعة بيعيّن عميد.</summary>
        public async Task<AdminDto> RegisterFacultyAdminAsync(RegisterAdminDto registerDto, int actorId = 0)
        {
            Validate(registerDto);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

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
                CreatedBy = actorId,
                IsActive = true,
                UniversityId = faculty.UniversityId,
                FacultyId = registerDto.FacultyId,
                RoleId = role.Id,
                ProfileImage = "default.jpg"
            };

            await _unitOfWork.Repository<Admin>().AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            if (actorId > 0)
                await _audit.LogAsync(SuperEntry(actorId, ActivityActions.Create, faculty.Id, admin.Id,
                    $"Appointed dean {admin.FirstName} {admin.LastName} for faculty {faculty.Name}",
                    null, Snapshot(admin, ActivityEntities.Staff)));

            var createdAdmin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(admin.Id, a => a.University, a => a.Faculty, a => a.Role);

            var adminDto = _mapper.Map<AdminDto>(createdAdmin);
            adminDto.Role = "Admin";
            adminDto.UserType = "Admin";

            return adminDto;
        }

        #endregion

        #region Student Management

        public async Task<StudentDto> RegisterStudentAsync(RegisterStudentDto registerDto, int adminId)
        {
            Validate(registerDto);

            var admin = await GetAdminWithFaculty(adminId);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

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
            student.CreatedBy = admin.Id;
            student.IsActive = true;
            await _unitOfWork.Repository<Student>().AddAsync(student);
            await _unitOfWork.SaveChangesAsync();

            await _audit.LogAsync(Entry(admin, ActivityActions.Create, ActivityEntities.Student, student.Id,
                $"Added student {student.FirstName} {student.LastName}", null, Snapshot(student, ActivityEntities.Student)));

            var createdStudent = await _unitOfWork.Repository<Student>()
                .GetByIdAsync(student.Id,
                    s => s.University,
                    s => s.Faculty,
                    s => s.Department,
                    s => s.Role);

            return _mapper.Map<StudentDto>(createdStudent);
        }

        public async Task<List<StudentDto>> GetFacultyStudentsAsync(int adminId, bool includeDisabled = false)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var students = await _unitOfWork.Repository<Student>()
                .FindAsync(s => s.FacultyId == admin.FacultyId && (includeDisabled || s.IsActive),
                    s => s.University,
                    s => s.Faculty,
                    s => s.Department,
                    s => s.Role);

            return _mapper.Map<List<StudentDto>>(students);
        }

        public async Task<StudentDto> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, int adminId)
        {
            Validate(updateDto);

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

            var before = Snapshot(student, ActivityEntities.Student);
            var wasActive = student.IsActive;

            student.FirstName = updateDto.FirstName;
            student.LastName = updateDto.LastName;
            student.PhoneNumber = updateDto.PhoneNumber;
            student.DepartmentId = updateDto.DepartmentId;
            student.Year = updateDto.Year;
            student.IsActive = updateDto.IsActive;
            student.UpdatedAt = DateTime.UtcNow;
            student.UpdatedBy = admin.Id;

            _audit.Stage(Entry(admin, ActionFor(wasActive, student.IsActive), ActivityEntities.Student, student.Id,
                $"Updated student {student.FirstName} {student.LastName}", before, Snapshot(student, ActivityEntities.Student)));

            await _unitOfWork.Repository<Student>().UpdateAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StudentDto>(student);
        }

        public Task<bool> DisableStudentAsync(int studentId, int adminId) =>
            ChangeMemberStateAsync<Student>(studentId, adminId, ActivityEntities.Student, ActivityActions.Disable);

        public Task<bool> EnableStudentAsync(int studentId, int adminId) =>
            ChangeMemberStateAsync<Student>(studentId, adminId, ActivityEntities.Student, ActivityActions.Enable);

        public Task<bool> DeleteStudentAsync(int studentId, int adminId) =>
            ChangeMemberStateAsync<Student>(studentId, adminId, ActivityEntities.Student, ActivityActions.Delete);

        #endregion

        #region Doctor Management

        public async Task<DoctorDto> RegisterDoctorAsync(RegisterDoctorDto registerDto, int adminId)
        {
            try
            {
                Validate(registerDto);

                var admin = await GetAdminWithFaculty(adminId);

                await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

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
                doctor.CreatedBy = admin.Id;
                doctor.IsActive = true;
                await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
                await _unitOfWork.SaveChangesAsync();

                await _audit.LogAsync(Entry(admin, ActivityActions.Create, ActivityEntities.Doctor, doctor.Id,
                    $"Added doctor {doctor.FirstName} {doctor.LastName}", null, Snapshot(doctor, ActivityEntities.Doctor)));

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

        public async Task<List<DoctorDto>> GetFacultyDoctorsAsync(int adminId, bool includeDisabled = false)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var doctors = await _unitOfWork.Repository<Doctor>()
                .FindAsync(d => d.FacultyId == admin.FacultyId && (includeDisabled || d.IsActive),
                    d => d.University,
                    d => d.Faculty,
                    d => d.Department,
                    d => d.Role);

            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        public async Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int adminId)
        {
            Validate(updateDto);

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

            var before = Snapshot(doctor, ActivityEntities.Doctor);
            var wasActive = doctor.IsActive;

            doctor.FirstName = updateDto.FirstName;
            doctor.LastName = updateDto.LastName;
            doctor.PhoneNumber = updateDto.PhoneNumber;
            doctor.DepartmentId = updateDto.DepartmentId;
            doctor.Specialization = updateDto.Specialization;
            doctor.AcademicDegree = updateDto.AcademicDegree;
            doctor.YearsOfExperience = updateDto.YearsOfExperience;
            doctor.IsActive = updateDto.IsActive;
            doctor.UpdatedAt = DateTime.UtcNow;
            doctor.UpdatedBy = admin.Id;

            _audit.Stage(Entry(admin, ActionFor(wasActive, doctor.IsActive), ActivityEntities.Doctor, doctor.Id,
                $"Updated doctor {doctor.FirstName} {doctor.LastName}", before, Snapshot(doctor, ActivityEntities.Doctor)));

            await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DoctorDto>(doctor);
        }

        public Task<bool> DisableDoctorAsync(int doctorId, int adminId) =>
            ChangeMemberStateAsync<Doctor>(doctorId, adminId, ActivityEntities.Doctor, ActivityActions.Disable);

        public Task<bool> EnableDoctorAsync(int doctorId, int adminId) =>
            ChangeMemberStateAsync<Doctor>(doctorId, adminId, ActivityEntities.Doctor, ActivityActions.Enable);

        public Task<bool> DeleteDoctorAsync(int doctorId, int adminId) =>
            ChangeMemberStateAsync<Doctor>(doctorId, adminId, ActivityEntities.Doctor, ActivityActions.Delete);

        #endregion

        #region Proctor Management

        public async Task<ProctorDto> RegisterProctorAsync(RegisterProctorDto registerDto, int adminId)
        {
            try
            {
                Validate(registerDto);

                var admin = await GetAdminWithFaculty(adminId);

                await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

                var role = (await _unitOfWork.Repository<Role>()
                    .FindAsync(r => r.Name == "Proctor"))
                    .FirstOrDefault() ?? throw new Exception("Proctor role not found");

                var proctor = _mapper.Map<Proctor>(registerDto);
                proctor.PasswordHash = HashPassword(registerDto.Password);
                proctor.RoleId = role.Id;
                proctor.FacultyId = admin.FacultyId;     // كل كلية ليها مراقبينها
                proctor.ProfileImage = "default.jpg";
                proctor.CreatedAt = DateTime.UtcNow;
                proctor.CreatedBy = admin.Id;
                proctor.IsActive = true;
                proctor.IsOnDuty = false;
                proctor.ActiveSessionsCount = 0;
                proctor.AverageResponseTime = 0.0;

                await _unitOfWork.Repository<Proctor>().AddAsync(proctor);
                await _unitOfWork.SaveChangesAsync();

                await _audit.LogAsync(Entry(admin, ActivityActions.Create, ActivityEntities.Proctor, proctor.Id,
                    $"Added proctor {proctor.FirstName} {proctor.LastName}", null, Snapshot(proctor, ActivityEntities.Proctor)));

                var createdProctor = await _unitOfWork.Repository<Proctor>()
                    .GetByIdAsync(proctor.Id, p => p.Role);

                return _mapper.Map<ProctorDto>(createdProctor);
            }
            catch (DbUpdateException dbEx)
            {
                var fullError = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new Exception($"Database update error: {fullError}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error registering proctor: {ex.Message}");
            }
        }

        public async Task<List<ProctorDto>> GetFacultyProctorsAsync(int adminId, bool includeDisabled = false)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var proctors = await _unitOfWork.Repository<Proctor>()
                .FindAsync(p => p.FacultyId == admin.FacultyId && (includeDisabled || p.IsActive), p => p.Role);

            return _mapper.Map<List<ProctorDto>>(proctors);
        }

        public async Task<ProctorDto> UpdateProctorAsync(int proctorId, UpdateProctorDto updateDto, int adminId)
        {
            Validate(updateDto);

            var admin = await GetAdminWithFaculty(adminId);

            var proctor = await _unitOfWork.Repository<Proctor>()
                .GetByIdAsync(proctorId, p => p.Role);

            if (proctor == null || proctor.FacultyId != admin.FacultyId)
                throw new Exception("Proctor not found or access denied");

            var before = Snapshot(proctor, ActivityEntities.Proctor);
            var wasActive = proctor.IsActive;

            proctor.FirstName = updateDto.FirstName;
            proctor.LastName = updateDto.LastName;
            proctor.PhoneNumber = updateDto.PhoneNumber;
            proctor.Department = updateDto.Department;
            proctor.IsActive = updateDto.IsActive;
            proctor.UpdatedAt = DateTime.UtcNow;
            proctor.UpdatedBy = admin.Id;

            _audit.Stage(Entry(admin, ActionFor(wasActive, proctor.IsActive), ActivityEntities.Proctor, proctor.Id,
                $"Updated proctor {proctor.FirstName} {proctor.LastName}", before, Snapshot(proctor, ActivityEntities.Proctor)));

            await _unitOfWork.Repository<Proctor>().UpdateAsync(proctor);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProctorDto>(proctor);
        }

        public Task<bool> DisableProctorAsync(int proctorId, int adminId) =>
            ChangeMemberStateAsync<Proctor>(proctorId, adminId, ActivityEntities.Proctor, ActivityActions.Disable);

        public Task<bool> EnableProctorAsync(int proctorId, int adminId) =>
            ChangeMemberStateAsync<Proctor>(proctorId, adminId, ActivityEntities.Proctor, ActivityActions.Enable);

        public Task<bool> DeleteProctorAsync(int proctorId, int adminId) =>
            ChangeMemberStateAsync<Proctor>(proctorId, adminId, ActivityEntities.Proctor, ActivityActions.Delete);

        #endregion

        #region Faculty Staff (Secretary / Technical)

        public async Task<StaffDto> RegisterStaffAsync(RegisterStaffDto dto, int actorId)
        {
            Validate(dto);

            var actor = await GetAdminWithFaculty(actorId);
            var actorRole = StaffRules.RoleOf(actor);

            if (dto.Role != AppRoles.Secretary && dto.Role != AppRoles.Technical)
                throw new Exception("Invalid staff role");

            // منع التصعيد: الأمين يعيّن تيم فني بس، والعميد يعيّن أمين وتيم فني
            if (!StaffRules.CanManageStaff(actorRole, dto.Role))
                throw new Exception("You don't have permission to appoint this role");

            await EnsureUniqueAsync(dto.Email, dto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == dto.Role))
                .FirstOrDefault() ?? throw new Exception($"Role '{dto.Role}' not found");

            Admin staff = dto.Role == AppRoles.Secretary ? new FacultySecretary() : new TechnicalStaff();
            staff.FirstName = dto.FirstName;
            staff.LastName = dto.LastName;
            staff.Email = dto.Email;
            staff.PasswordHash = HashPassword(dto.Password);
            staff.NationalId = dto.NationalId;
            staff.PhoneNumber = dto.PhoneNumber;
            staff.ProfileImage = "default.jpg";
            staff.CreatedAt = DateTime.UtcNow;
            staff.CreatedBy = actor.Id;
            staff.IsActive = true;
            staff.RoleId = role.Id;
            staff.UniversityId = actor.UniversityId;
            staff.FacultyId = actor.FacultyId;

            await _unitOfWork.Repository<Admin>().AddAsync(staff);
            await _unitOfWork.SaveChangesAsync();

            await _audit.LogAsync(Entry(actor, ActivityActions.Create, ActivityEntities.Staff, staff.Id,
                $"Appointed {dto.Role} {staff.FirstName} {staff.LastName}", null, Snapshot(staff, ActivityEntities.Staff)));

            staff.Faculty = actor.Faculty;
            return ToStaffDto(staff, actorRole);
        }

        public async Task<List<StaffDto>> GetStaffAsync(int actorId, bool includeDisabled = true)
        {
            var actor = await GetAdminWithFaculty(actorId);
            var actorRole = StaffRules.RoleOf(actor);

            var staff = await _unitOfWork.Repository<Admin>()
                .FindAsync(a => a.FacultyId == actor.FacultyId
                                && (a is FacultySecretary || a is TechnicalStaff)
                                && (includeDisabled || a.IsActive),
                    a => a.Faculty);

            return staff.Select(a => ToStaffDto(a, actorRole)).ToList();
        }

        public async Task<StaffDto> UpdateStaffAsync(int staffId, UpdateStaffDto dto, int actorId)
        {
            Validate(dto);

            var actor = await GetAdminWithFaculty(actorId);
            var staff = await GetManagedStaff(staffId, actor);

            var before = Snapshot(staff, ActivityEntities.Staff);
            var wasActive = staff.IsActive;

            staff.FirstName = dto.FirstName;
            staff.LastName = dto.LastName;
            staff.PhoneNumber = dto.PhoneNumber;
            staff.IsActive = dto.IsActive;
            staff.UpdatedAt = DateTime.UtcNow;
            staff.UpdatedBy = actor.Id;

            _audit.Stage(Entry(actor, ActionFor(wasActive, staff.IsActive), ActivityEntities.Staff, staff.Id,
                $"Updated {StaffRules.RoleOf(staff)} {staff.FirstName} {staff.LastName}", before, Snapshot(staff, ActivityEntities.Staff)));

            await _unitOfWork.Repository<Admin>().UpdateAsync(staff);
            await _unitOfWork.SaveChangesAsync();

            return ToStaffDto(staff, StaffRules.RoleOf(actor));
        }

        public Task<bool> DisableStaffAsync(int staffId, int actorId) =>
            ChangeStaffStateAsync(staffId, actorId, ActivityActions.Disable);

        public Task<bool> EnableStaffAsync(int staffId, int actorId) =>
            ChangeStaffStateAsync(staffId, actorId, ActivityActions.Enable);

        public Task<bool> DeleteStaffAsync(int staffId, int actorId) =>
            ChangeStaffStateAsync(staffId, actorId, ActivityActions.Delete);

        private async Task<bool> ChangeStaffStateAsync(int staffId, int actorId, string action)
        {
            var actor = await GetAdminWithFaculty(actorId);
            var staff = await GetManagedStaff(staffId, actor);

            var before = Snapshot(staff, ActivityEntities.Staff);
            ApplyState(staff, action, actor.Id);

            _audit.Stage(Entry(actor, action, ActivityEntities.Staff, staff.Id,
                $"{action} {StaffRules.RoleOf(staff)} {staff.FirstName} {staff.LastName}", before,
                action == ActivityActions.Delete ? null : Snapshot(staff, ActivityEntities.Staff)));

            await _unitOfWork.Repository<Admin>().UpdateAsync(staff);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>يجيب عضو تيم أو أمين من نفس كلية المنفّذ، ويتأكد إن الدور بتاعه يسمح للمنفّذ يديره.</summary>
        private async Task<Admin> GetManagedStaff(int staffId, Admin actor)
        {
            var staff = await _unitOfWork.Repository<Admin>().GetByIdAsync(staffId);

            if (staff == null || staff.FacultyId != actor.FacultyId || staff.Id == actor.Id
                || !(staff is FacultySecretary || staff is TechnicalStaff))
                throw new Exception("Staff member not found or access denied");

            if (!StaffRules.CanManageStaff(StaffRules.RoleOf(actor), StaffRules.RoleOf(staff)))
                throw new Exception("You don't have permission to manage this staff member");

            return staff;
        }

        private static StaffDto ToStaffDto(Admin a, string actorRole) => new()
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            Email = a.Email,
            NationalId = a.NationalId,
            PhoneNumber = a.PhoneNumber,
            Role = StaffRules.RoleOf(a),
            IsActive = a.IsActive,
            FacultyId = a.FacultyId,
            FacultyName = a.Faculty?.Name ?? string.Empty,
            LastLogin = a.LastLogin,
            CreatedAt = a.CreatedAt,
            CanManage = StaffRules.CanManageStaff(actorRole, StaffRules.RoleOf(a))
        };

        #endregion

        #region Revert (students / doctors / proctors / staff)

        public async Task<string> RevertMemberActionAsync(int logId, int actorId)
        {
            var actor = await GetAdminWithFaculty(actorId);
            var role = StaffRules.RoleOf(actor);

            if (!StaffRules.CanDeleteMembers(role))
                throw new Exception("Only the dean or the faculty secretary can undo actions");

            var log = await _audit.GetByIdAsync(logId) ?? throw new Exception("Log entry not found");

            if (log.FacultyId != actor.FacultyId)
                throw new Exception("Log entry not found or access denied");
            if (log.IsReverted)
                throw new Exception("This action was already undone");
            if (log.EntityId == null || !Tracked.ContainsKey(log.EntityType)
                || !ActivityActions.IsReversible(log.EntityType, log.Action))
                throw new Exception("This action can't be undone here");

            // _db.Users مفيهوش فلتر الحذف، فبنقدر نوصل للمحذوفين
            var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == log.EntityId.Value)
                ?? throw new Exception("The record no longer exists");

            var typeOk = log.EntityType switch
            {
                ActivityEntities.Student => target is Student,
                ActivityEntities.Doctor => target is Doctor,
                ActivityEntities.Proctor => target is Proctor,
                ActivityEntities.Staff => target is FacultySecretary || target is TechnicalStaff,
                _ => false
            };
            if (!typeOk || FacultyOf(target) != actor.FacultyId)
                throw new Exception("Record not found or access denied");

            if (log.EntityType == ActivityEntities.Staff && !StaffRules.CanManageStaff(role, StaffRules.RoleOf(target)))
                throw new Exception("You don't have permission to manage this staff member");

            var now = DateTime.UtcNow;
            string summary;

            switch (log.Action)
            {
                case ActivityActions.Create:
                case ActivityActions.Restore:
                    if (target.IsDeleted) throw new Exception("The record is already deleted");
                    target.IsDeleted = true;
                    target.DeletedAt = now;
                    target.DeletedBy = actor.Id;
                    summary = "Undid creation (record removed)";
                    break;

                case ActivityActions.Delete:
                    if (!target.IsDeleted) throw new Exception("The record is already restored");
                    target.IsDeleted = false;
                    target.DeletedAt = null;
                    target.DeletedBy = null;
                    summary = "Restored deleted record";
                    break;

                default: // Update / Disable / Enable
                    if (target.IsDeleted) throw new Exception("The record is deleted. Undo the delete first");
                    if (string.IsNullOrEmpty(log.OldValues)) throw new Exception("No previous values were recorded");
                    ApplySnapshot(target, log.OldValues, log.EntityType);
                    summary = "Restored previous values";
                    break;
            }

            target.UpdatedAt = now;
            target.UpdatedBy = actor.Id;

            _audit.Stage(Entry(actor, RevertAction, log.EntityType, log.EntityId,
                $"{summary}: {log.Description}"));
            _audit.MarkReverted(log, actor.Id);

            await _unitOfWork.SaveChangesAsync();
            return summary;
        }

        #endregion

        #region Change Password Methods

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                Validate(changePasswordDto);

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

        #region Support Management (SuperAdmin only)

        public async Task<SupportDto> RegisterSupportAsync(RegisterSupportDto registerDto)
        {
            Validate(registerDto);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

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
            Validate(updateDto);

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

        #region Dean Management (SuperAdmin only)

        public async Task<List<AdminDto>> GetAllAdminsAsync()
        {
            // العمداء بس: الأمناء والتيم الفني بيتديروا من داخل الكلية
            var admins = await _unitOfWork.Repository<Admin>()
                .FindAsync(a => a.IsActive && !(a is FacultySecretary) && !(a is TechnicalStaff),
                    a => a.University,
                    a => a.Faculty,
                    a => a.Role);

            return _mapper.Map<List<AdminDto>>(admins);
        }

        public async Task<AdminDto> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto, int actorId = 0)
        {
            try
            {
                Validate(updateDto);

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

                if (admin == null || admin is FacultySecretary || admin is TechnicalStaff)
                    throw new Exception("Admin not found");

                var before = Snapshot(admin, ActivityEntities.Staff);

                admin.FirstName = updateDto.FirstName;
                admin.LastName = updateDto.LastName;
                admin.PhoneNumber = updateDto.PhoneNumber;
                admin.UniversityId = updateDto.UniversityId;
                admin.FacultyId = updateDto.FacultyId;
                admin.IsActive = updateDto.IsActive;
                admin.UpdatedAt = DateTime.UtcNow;
                admin.UpdatedBy = actorId > 0 ? actorId : null;

                if (actorId > 0)
                    _audit.Stage(SuperEntry(actorId, ActivityActions.Update, admin.FacultyId, admin.Id,
                        $"Updated dean {admin.FirstName} {admin.LastName}", before, Snapshot(admin, ActivityEntities.Staff)));

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

        public async Task<bool> DeleteAdminAsync(int adminId, int actorId = 0)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId);

            if (admin == null || admin is FacultySecretary || admin is TechnicalStaff)
                throw new Exception("Admin not found");

            admin.IsActive = false;
            admin.UpdatedAt = DateTime.UtcNow;

            if (actorId > 0)
                _audit.Stage(SuperEntry(actorId, ActivityActions.Disable, admin.FacultyId, admin.Id,
                    $"Removed dean {admin.FirstName} {admin.LastName}"));

            await _unitOfWork.Repository<Admin>().UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Helper Methods

        private static void Validate(object dto)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
            {
                var errors = string.Join(", ", results.Select(v => v.ErrorMessage));
                throw new Exception($"Validation failed: {errors}");
            }
        }

        /// <summary>
        /// بنفحص جدول Users مباشرة (مش الـ Repository) عشان المحذوفين كمان،
        /// لأن الـ unique index على الإيميل والرقم القومي لسه حاجزهم.
        /// </summary>
        private async Task EnsureUniqueAsync(string email, string nationalId)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email || u.NationalId == nationalId))
                throw new Exception("User with this email or national ID already exists");
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

        private static int FacultyOf(User u) => u switch
        {
            Student s => s.FacultyId,
            Doctor d => d.FacultyId,
            Proctor p => p.FacultyId ?? 0,
            Admin a => a.FacultyId,
            _ => 0
        };

        private static string ActionFor(bool wasActive, bool isActive) =>
            wasActive == isActive ? ActivityActions.Update
            : isActive ? ActivityActions.Enable : ActivityActions.Disable;

        private static void ApplyState(User target, string action, int actorId)
        {
            switch (action)
            {
                case ActivityActions.Disable:
                    if (!target.IsActive) throw new Exception("Already disabled");
                    target.IsActive = false;
                    break;
                case ActivityActions.Enable:
                    if (target.IsActive) throw new Exception("Already active");
                    target.IsActive = true;
                    break;
                case ActivityActions.Delete:
                    target.IsDeleted = true;
                    target.DeletedAt = DateTime.UtcNow;
                    target.DeletedBy = actorId;
                    break;
            }
            target.UpdatedAt = DateTime.UtcNow;
            target.UpdatedBy = actorId;
        }

        /// <summary>تعطيل / تفعيل / حذف طالب أو دكتور أو مراقب. الحذف للعميد والأمين بس.</summary>
        private async Task<bool> ChangeMemberStateAsync<T>(int id, int actorId, string entityType, string action) where T : User
        {
            var actor = await GetAdminWithFaculty(actorId);

            if (action == ActivityActions.Delete && !StaffRules.CanDeleteMembers(StaffRules.RoleOf(actor)))
                throw new Exception("You don't have permission to delete. You can disable the account instead.");

            var target = await _unitOfWork.Repository<T>().GetByIdAsync(id);
            if (target == null || FacultyOf(target) != actor.FacultyId)
                throw new Exception($"{entityType} not found or access denied");

            var before = Snapshot(target, entityType);
            ApplyState(target, action, actor.Id);

            _audit.Stage(Entry(actor, action, entityType, target.Id,
                $"{action} {entityType.ToLower()} {target.FirstName} {target.LastName}", before,
                action == ActivityActions.Delete ? null : Snapshot(target, entityType)));

            await _unitOfWork.Repository<T>().UpdateAsync(target);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // ---- Audit helpers ----

        private AuditEntry Entry(Admin actor, string action, string entityType, int? entityId, string description,
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

        private AuditEntry SuperEntry(int actorId, string action, int facultyId, int? entityId, string description,
            object? oldValues = null, object? newValues = null) => new()
        {
            ActorId = actorId,
            ActorRole = AppRoles.SuperAdmin,
            FacultyId = facultyId,
            Action = action,
            EntityType = ActivityEntities.Staff,
            EntityId = entityId,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues
        };

        private static Dictionary<string, object?> Snapshot(object entity, string entityType)
        {
            var type = entity.GetType();
            var d = new Dictionary<string, object?>();
            foreach (var name in Tracked[entityType])
                d[name] = type.GetProperty(name)?.GetValue(entity);
            return d;
        }

        private static void ApplySnapshot(object target, string json, string entityType)
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (!Tracked[entityType].Contains(p.Name)) continue;
                var prop = target.GetType().GetProperty(p.Name);
                if (prop == null || !prop.CanWrite) continue;
                prop.SetValue(target, JsonSerializer.Deserialize(p.Value.GetRawText(), prop.PropertyType));
            }
        }

        // ---- Mapping helpers ----

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

            var dto = _mapper.Map<AdminDto>(admin);
            var role = StaffRules.RoleOf(admin);   // Admin / FacultySecretary / TechnicalStaff
            dto.Role = role;
            dto.UserType = role;
            return dto;
        }

        private UserDto MapProctorWithAllData(Proctor proctor) => _mapper.Map<ProctorDto>(proctor);

        private UserDto MapSupportWithAllData(Support support) => _mapper.Map<SupportDto>(support);

        private UserDto MapSuperAdminWithAllData(SuperAdmin superAdmin)
        {
            var dto = _mapper.Map<AdminDto>(superAdmin);
            dto.Role = "SuperAdmin";
            dto.UserType = "SuperAdmin";
            return dto;
        }

        // ---- Passwords & tokens ----

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
            var userType = StaffRules.RoleOf(user);

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
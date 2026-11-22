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
using TruthEye.Exceptions;
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
        private readonly IEmailSender _email;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly TwoFactorCrypto _twoFactor;

        private const int MaxFailures = 5;              // لكل (حساب + IP)
        private const int MaxFailuresPerIp = 30;        // لكل IP على كل الحسابات
        private const int MaxFailuresPerAccount = 40;   // لكل حساب من كل الـ IPs
        private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
        private const string RevertAction = "Revert";
        private static readonly TimeSpan SessionAlive = TimeSpan.FromSeconds(90);

        // الحقول اللي بتتسجّل في السجل وبيتراجع عنها
        private static readonly Dictionary<string, string[]> Tracked = new()
        {
            [ActivityEntities.Student] = new[] { "FirstName", "LastName", "PhoneNumber", "DepartmentId", "Year", "IsActive" },
            [ActivityEntities.Doctor] = new[] { "FirstName", "LastName", "PhoneNumber", "DepartmentId", "Specialization", "AcademicDegree", "YearsOfExperience", "IsActive" },
            [ActivityEntities.Proctor] = new[] { "FirstName", "LastName", "PhoneNumber", "Department", "IsActive" },
            [ActivityEntities.Staff] = new[] { "FirstName", "LastName", "PhoneNumber", "IsActive" },
        };

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, AppDbContext db, ISystemLogService audit, IEmailSender email)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _db = db;
            _audit = audit;
            _email = email;
            _twoFactor = new TwoFactorCrypto(configuration["Jwt:Key"] ?? string.Empty);
        }

        #region Authentication Methods

        public async Task<LoginOutcome> BeginLoginAsync(LoginDto loginDto, string? ip = null, string? deviceId = null)
        {
            Validate(loginDto);

            if (string.IsNullOrEmpty(loginDto.Email) && string.IsNullOrEmpty(loginDto.NationalId))
                throw new BadRequestException("Either email or national ID must be provided", "AUTH_INVALID_CREDENTIALS");

            var account = (string.IsNullOrWhiteSpace(loginDto.Email) ? loginDto.NationalId : loginDto.Email)!
                .Trim().ToLowerInvariant();
            if (account.Length > 100) account = account[..100];
            ip = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;
            deviceId = string.IsNullOrWhiteSpace(deviceId) ? "none" : deviceId;

            // التلات عدّادات: حساب+IP (يوقف المخمّن)، IP لوحده (يوقف الرش على حسابات كتير)، حساب لوحده (يوقف الهجوم الموزّع)
            var counters = new[]
            {
                (Key: $"login:{account}:{ip}", Max: MaxFailures),
                (Key: $"ip:{ip}", Max: MaxFailuresPerIp),
                (Key: $"acct:{account}", Max: MaxFailuresPerAccount)
            };
            var names = counters.Select(c => c.Key).ToList();
            var locks = await _db.LoginLockouts.Where(l => names.Contains(l.Key)).ToListAsync();

            var now = DateTime.UtcNow;
            var until = locks.Where(l => l.LockedUntil > now).Select(l => l.LockedUntil!.Value)
                .DefaultIfEmpty().Max();
            if (until > now)
            {
                var mins = (int)Math.Ceiling((until - now).TotalMinutes);
                throw new BadRequestException($"Too many failed attempts. Try again in {mins} minute(s).", "AUTH_LOCKED");
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
                foreach (var c in counters)
                    RegisterFailure(c.Key, locks.FirstOrDefault(l => l.Key == c.Key), c.Max, now);

                try { await _db.SaveChangesAsync(); }
                catch (DbUpdateException)
                {
                    // محاولتين في نفس اللحظة عملوا نفس المفتاح: الـ unique index بيكسب والعدّاد بيفوّت tick واحد
                    _db.ChangeTracker.Clear();
                }

                var lockedNow = locks.Any(l => l.LockedUntil > now);
                await LogLoginAsync(ActivityActions.LoginFailed, user, account, ip,
                    lockedNow ? "Failed login, account locked" : "Failed login");

                throw new BadRequestException("Invalid credentials", "AUTH_INVALID_CREDENTIALS");
            }

            // دخول ناجح: نصفّر عدّاد (حساب+IP) بس. عدّاد الـ IP العام بيفضل لحد ما ينتهي
            var own = locks.FirstOrDefault(l => l.Key == counters[0].Key);
            if (own != null)
            {
                _db.LoginLockouts.Remove(own);
                await _db.SaveChangesAsync();
            }

            if (!user.IsActive)
            {
                await LogLoginAsync(ActivityActions.LoginDisabled, user, account, ip, "Login attempt on a disabled account");
                throw new ForbiddenException("Account is disabled", "AUTH_DISABLED");
            }

            // جلسة حية من جهاز تاني؟ الدخول يترفض
            var alive = now - SessionAlive;
            var liveOthers = await _db.UserSessions.AsNoTracking()
                .AnyAsync(s => s.UserId == user.Id && s.EndedAt == null && s.LastSeenAt > alive && s.DeviceId != deviceId);

            if (liveOthers)
            {
                if (user is Student)
                {
                    // لو بيمتحن دلوقتي: ندوّن المحاولة كدليل في جلسته
                    var liveSessions = await _db.ExamSessions
                        .Where(s => s.StudentId == user.Id && s.Status == "InProgress")
                        .ToListAsync();
                    foreach (var live in liveSessions)
                    {
                        _db.AILogs.Add(new AILog { ExamSessionId = live.Id, EventType = "DuplicateLogin", Timestamp = now, ScreenshotPath = "", Confidence = 1 });
                        live.RiskScore += RiskCalculator.WeightOf("DuplicateLogin");
                        live.ViolationCount += 1;
                    }
                    if (liveSessions.Count > 0) await _db.SaveChangesAsync();
                }

                await LogLoginAsync(ActivityActions.LoginBlocked, user, account, ip,
                    "Blocked: the account is already signed in on another device");
                throw new ConflictException("This account is signed in on another device right now.", "AUTH_OTHER_DEVICE");
            }

            // 2FA مفعّل: الباسورد اتقبل، بس التوكنات لسه ما اتصدرتش. الكود بيتطلب في الخطوة التانية
            if (user.TwoFactorEnabled)
                return new LoginOutcome(null, _twoFactor.IssueChallenge(user.Id));

            var tokens = await OpenSessionAsync(user, account, ip, deviceId, now);
            return new LoginOutcome(tokens, null);
        }

        public async Task<TokenPair> LoginAsync(LoginDto loginDto, string? ip = null, string? deviceId = null)
        {
            var outcome = await BeginLoginAsync(loginDto, ip, deviceId);
            return outcome.Tokens ?? throw new BadRequestException("Two-factor code required", "AUTH_2FA_REQUIRED");
        }

        private async Task<TokenPair> OpenSessionAsync(User user, string account, string ip, string deviceId, DateTime now)
        {
            // نفس الجهاز أو مفيش جلسة حية: نقفل القديم ونفتح جلسة جديدة
            var olds = await _db.UserSessions.Where(s => s.UserId == user.Id && s.EndedAt == null).ToListAsync();
            foreach (var s in olds) s.EndedAt = now;

            var sid = Guid.NewGuid().ToString("N");
            _db.UserSessions.Add(new UserSession
            {
                UserId = user.Id, Sid = sid, DeviceId = deviceId, Ip = ip, CreatedAt = now, LastSeenAt = now
            });

            // الجلسات القديمة ما تقدرش تجدد
            await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ForEachAsync(t => t.RevokedAt = now);

            user.LastLogin = now;
            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await LogLoginAsync(ActivityActions.LoginSuccess, user, account, ip, "Signed in");

            return new TokenPair(GenerateJwtToken(user, sid), await IssueRefreshAsync(user.Id, sid), user.MustChangePassword);
        }

        /// <summary>الخطوة التانية: الباسورد اتقبل قبل كده، والكود (TOTP أو كود استرجاع) بيتأكد هنا وبعدين بتتفتح الجلسة.</summary>
        public async Task<TokenPair> CompleteTwoFactorLoginAsync(string challengeToken, string code, string? ip = null, string? deviceId = null)
        {
            var userId = _twoFactor.ReadChallenge(challengeToken);
            if (userId == null)
                throw new BadRequestException("The sign-in step expired. Please sign in again.", "AUTH_CHALLENGE_EXPIRED");

            var user = await GetActiveUserAsync(userId.Value);
            ip = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;
            deviceId = string.IsNullOrWhiteSpace(deviceId) ? "none" : deviceId;
            var account = (user.Email ?? string.Empty).Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            // نفس عدّاد الباسورد: كود 2FA غلط كتير يقفل الحساب مؤقتاً
            var key = $"2fa:{user.Id}";
            var lockRow = await _db.LoginLockouts.FirstOrDefaultAsync(l => l.Key == key);
            if (lockRow?.LockedUntil > now)
            {
                var mins = (int)Math.Ceiling((lockRow.LockedUntil!.Value - now).TotalMinutes);
                throw new BadRequestException($"Too many failed attempts. Try again in {mins} minute(s).", "AUTH_LOCKED");
            }

            if (!ConsumeCode(user, code, now))
            {
                RegisterFailure(key, lockRow, MaxFailures, now);
                await _db.SaveChangesAsync();
                await LogLoginAsync(ActivityActions.LoginFailed, user, account, ip, "Wrong two-factor code");
                throw new BadRequestException("Invalid verification code", "AUTH_INVALID_2FA_CODE");
            }

            if (lockRow != null) _db.LoginLockouts.Remove(lockRow);
            await _db.SaveChangesAsync();   // يحفظ الـ step المستخدم أو كود الاسترجاع اللي اتصرف

            // الجلسة الحية على جهاز تاني ممكن تكون اتفتحت وإحنا مستنيين الكود
            var alive = now - SessionAlive;
            var liveOthers = await _db.UserSessions.AsNoTracking()
                .AnyAsync(s => s.UserId == user.Id && s.EndedAt == null && s.LastSeenAt > alive && s.DeviceId != deviceId);
            if (liveOthers)
                throw new ConflictException("This account is signed in on another device right now.", "AUTH_OTHER_DEVICE");

            return await OpenSessionAsync(user, account, ip, deviceId, now);
        }

        // ---------- الإعداد والإلغاء ----------

        public async Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(int userId)
        {
            var user = await GetActiveUserAsync(userId);
            if (user.TwoFactorEnabled)
                throw new BadRequestException("Two-factor sign-in is already on", "AUTH_2FA_ALREADY_ON");

            var secret = TotpCodes.NewSecret();
            user.TotpSecretProtected = _twoFactor.EncryptSecret(secret);
            user.TotpLastStep = null;
            await _unitOfWork.SaveChangesAsync();

            var base32 = TotpCodes.ToBase32(secret);
            return new TwoFactorSetupDto(base32, TotpCodes.OtpAuthUri(user.Email, base32));
        }

        public async Task<IReadOnlyList<string>> EnableTwoFactorAsync(int userId, string code)
        {
            var user = await GetActiveUserAsync(userId);
            if (user.TwoFactorEnabled)
                throw new BadRequestException("Two-factor sign-in is already on", "AUTH_2FA_ALREADY_ON");
            if (user.TotpSecretProtected == null)
                throw new BadRequestException("Start the two-factor setup first", "AUTH_2FA_NOT_STARTED");

            var now = DateTime.UtcNow;
            var step = TotpCodes.Match(_twoFactor.DecryptSecret(user.TotpSecretProtected),
                TwoFactorCrypto.NormalizeCode(code), null, TotpCodes.StepAt(now));
            if (step == null)
                throw new BadRequestException("Invalid verification code", "AUTH_INVALID_2FA_CODE");

            var codes = TwoFactorCrypto.NewRecoveryCodes();
            user.TwoFactorEnabled = true;
            user.TotpLastStep = step;
            user.RecoveryCodeHashes = string.Join(',', codes.Select(TwoFactorCrypto.HashRecoveryCode));

            await SaveWithAuditAsync(user, userId, StaffRules.RoleOf(user), ActivityActions.EnableTwoFactor, "Turned on two-factor sign-in for");
            return codes;
        }

        public async Task DisableTwoFactorAsync(int userId, string password, string code)
        {
            var user = await GetActiveUserAsync(userId);
            if (!user.TwoFactorEnabled)
                throw new BadRequestException("Two-factor sign-in is not on", "AUTH_2FA_NOT_ON");
            if (!VerifyPassword(password, user.PasswordHash))
                throw new BadRequestException("Invalid credentials", "AUTH_INVALID_CREDENTIALS");
            if (!ConsumeCode(user, code, DateTime.UtcNow))
                throw new BadRequestException("Invalid verification code", "AUTH_INVALID_2FA_CODE");

            ClearTwoFactor(user);
            await SaveWithAuditAsync(user, userId, StaffRules.RoleOf(user), ActivityActions.DisableTwoFactor, "Turned off two-factor sign-in for");
        }

        public async Task<bool> IsTwoFactorEnabledAsync(int userId) =>
            (await GetActiveUserAsync(userId)).TwoFactorEnabled;

        /// <summary>رئيس الجامعة لأي حساب، والعميد لكليته بس. الأمين والتيم الفني والدعم ما يقدروش.</summary>
        public async Task ResetTwoFactorAsync(int targetId, int actorId)
        {
            var actor = await GetActiveUserAsync(actorId);
            var actorRole = StaffRules.RoleOf(actor);
            if (actorRole is not (AppRoles.SuperAdmin or AppRoles.Dean))
                throw new ForbiddenException();
            if (targetId == actorId)
                throw new BadRequestException("Use the disable option for your own account", "AUTH_2FA_SELF");

            var target = await _unitOfWork.Repository<User>().GetByIdAsync(targetId)
                ?? throw new NotFoundException("User not found");
            if (actorRole == AppRoles.Dean && ChatRules.FacultyOf(target) != ChatRules.FacultyOf(actor))
                throw new NotFoundException("User not found");   // خارج الكلية = "مش موجود"

            ClearTwoFactor(target);
            await SaveWithAuditAsync(target, actorId, actorRole, ActivityActions.ResetTwoFactor, "Reset two-factor sign-in for");
        }

        /// <summary>
        /// وضع الاسترجاع: يتشغّل من سطر الأوامر على السيرفر نفسه بس، مفيش endpoint.
        /// بيلغي الـ 2FA والجلسات ومحاولات الدخول لحساب رئيس الجامعة، ويرجّع رابط إعادة تعيين باسورد مرة واحدة.
        /// </summary>
        public async Task<string> RecoverSuperAdminAsync(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
            var admin = (await _unitOfWork.Repository<SuperAdmin>().GetAllAsync())
                .FirstOrDefault(a => a.Email.ToLower() == normalized)
                ?? throw new NotFoundException("No university president with this email");

            var now = DateTime.UtcNow;
            ClearTwoFactor(admin);

            await _db.RefreshTokens.Where(t => t.UserId == admin.Id && t.RevokedAt == null)
                .ForEachAsync(t => t.RevokedAt = now);
            await _db.UserSessions.Where(s => s.UserId == admin.Id && s.EndedAt == null)
                .ForEachAsync(s => s.EndedAt = now);
            await _db.PasswordResetTokens.Where(t => t.UserId == admin.Id && t.UsedAt == null)
                .ForEachAsync(t => t.UsedAt = now);

            var locks = await _db.LoginLockouts
                .Where(l => l.Key.Contains(normalized) || l.Key == "2fa:" + admin.Id)
                .ToListAsync();
            _db.LoginLockouts.RemoveRange(locks);

            var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = admin.Id,
                TokenHash = HashToken(raw),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(ResetTokenMinutes),
                RequestedBy = admin.Id
            });

            _audit.Stage(new AuditEntry
            {
                ActorId = admin.Id,
                ActorRole = AppRoles.SuperAdmin,
                Action = ActivityActions.ResetTwoFactor,
                EntityType = ActivityEntities.Staff,
                EntityId = admin.Id,
                Description = "Recovery from the server console: two-factor cleared, sessions ended, reset link issued"
            });
            await _unitOfWork.SaveChangesAsync();

            return $"{ResetPageUrl()}?token={raw}";
        }

        private async Task<User> GetActiveUserAsync(int userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
            if (user.IsDeleted || !user.IsActive)
                throw new ForbiddenException("Account is disabled", "AUTH_DISABLED");
            return user;
        }

        /// <summary>يتأكد من كود TOTP أو كود استرجاع، ويحدّث الحالة لو نجح. ما بيغيّرش حاجة لو فشل.</summary>
        private bool ConsumeCode(User user, string? code, DateTime now)
        {
            if (user.TotpSecretProtected == null) return false;
            var normalized = TwoFactorCrypto.NormalizeCode(code);
            if (normalized.Length == TotpCodes.Digits)
            {
                var step = TotpCodes.Match(_twoFactor.DecryptSecret(user.TotpSecretProtected),
                    normalized, user.TotpLastStep, TotpCodes.StepAt(now));
                if (step == null) return false;
                user.TotpLastStep = step;
                return true;
            }
            return TakeRecoveryCode(user, normalized);
        }

        private static bool TakeRecoveryCode(User user, string normalized)
        {
            if (user.RecoveryCodeHashes == null || normalized.Length != 8) return false;
            var hashes = user.RecoveryCodeHashes.Split(',').ToList();
            if (!hashes.Remove(TwoFactorCrypto.HashRecoveryCode(normalized))) return false;
            user.RecoveryCodeHashes = hashes.Count == 0 ? null : string.Join(',', hashes);
            return true;
        }

        private static void ClearTwoFactor(User user)
        {
            user.TwoFactorEnabled = false;
            user.TotpSecretProtected = null;
            user.TotpLastStep = null;
            user.RecoveryCodeHashes = null;
        }

        private async Task SaveWithAuditAsync(User target, int actorId, string actorRole, string action, string text)
        {
            var faculty = ChatRules.FacultyOf(target);
            _audit.Stage(new AuditEntry
            {
                ActorId = actorId,
                ActorRole = actorRole,
                FacultyId = faculty > 0 ? faculty : null,
                Action = action,
                EntityType = StaffRules.RoleOf(target) switch
                {
                    AppRoles.Student => ActivityEntities.Student,
                    AppRoles.Doctor => ActivityEntities.Doctor,
                    AppRoles.Proctor => ActivityEntities.Proctor,
                    _ => ActivityEntities.Staff
                },
                EntityId = target.Id,
                Description = $"{text} {target.FirstName} {target.LastName}"
            });
            await _unitOfWork.SaveChangesAsync();
        }

        private void RegisterFailure(string key, LoginLockout? l, int max, DateTime now)
        {
            if (l == null)
            {
                l = new LoginLockout { Key = key };
                _db.LoginLockouts.Add(l);
            }
            else if ((l.LockedUntil != null && l.LockedUntil <= now) || now - l.UpdatedAt > FailureWindow)
            {
                // القفل خلص أو العدّاد قديم: عدّ جديد
                l.LockedUntil = null;
                l.Failures = 0;
            }

            l.Failures++;
            l.UpdatedAt = now;

            if (l.Failures >= max)
            {
                l.LockedUntil = now + LockFor;
                l.Failures = 0;
            }
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
                throw new NotFoundException("User not found");

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
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            if (await SuperAdminExistsAsync())
                throw new ForbiddenException("Setup already completed. A system admin already exists.");

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "SuperAdmin"))
                .FirstOrDefault() ?? throw new NotFoundException("Role 'SuperAdmin' not found");

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
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Admin"))
                .FirstOrDefault() ?? throw new NotFoundException("Role 'Admin' not found");

            var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(registerDto.FacultyId);
            if (faculty == null)
                throw new NotFoundException("Faculty not found");

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
                MustChangePassword = true,
                ProfileImage = "default.jpg"
            };

            await _unitOfWork.Repository<Admin>().AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();
            await SendSetupLinkIfRequestedAsync(registerDto.SendSetupLink, admin, actorId);

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
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            var admin = await GetAdminWithFaculty(adminId);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Student"))
                .FirstOrDefault() ?? throw new NotFoundException("Student role not found");

            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId);
            if (department == null)
                throw new NotFoundException("Department not found");

            if (department.FacultyId != admin.FacultyId)
                throw new ForbiddenException("Department does not belong to your faculty");

            var student = _mapper.Map<Student>(registerDto);
            student.PasswordHash = HashPassword(registerDto.Password);
            student.MustChangePassword = true;
            student.RoleId = role.Id;
            student.FacultyId = admin.FacultyId;
            student.UniversityId = admin.UniversityId;
            student.ProfileImage = "default.jpg";
            student.CreatedAt = DateTime.UtcNow;
            student.CreatedBy = admin.Id;
            student.IsActive = true;
            await _unitOfWork.Repository<Student>().AddAsync(student);
            await _unitOfWork.SaveChangesAsync();
            await SendSetupLinkIfRequestedAsync(registerDto.SendSetupLink, student, adminId);

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
                throw new NotFoundException("Student not found or access denied");

            if (updateDto.DepartmentId != student.DepartmentId)
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(updateDto.DepartmentId);
                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new ForbiddenException("Department does not belong to your faculty");
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
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            var admin = await GetAdminWithFaculty(adminId);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Doctor"))
                .FirstOrDefault() ?? throw new NotFoundException("Doctor role not found");

            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId);
            if (department == null)
                throw new NotFoundException("Department not found");

            if (department.FacultyId != admin.FacultyId)
                throw new ForbiddenException("Department does not belong to your faculty");

            var doctor = _mapper.Map<Doctor>(registerDto);
            doctor.PasswordHash = HashPassword(registerDto.Password);
            doctor.MustChangePassword = true;
            doctor.RoleId = role.Id;
            doctor.FacultyId = admin.FacultyId;
            doctor.UniversityId = admin.UniversityId;
            doctor.ProfileImage = "default.jpg";
            doctor.CreatedAt = DateTime.UtcNow;
            doctor.CreatedBy = admin.Id;
            doctor.IsActive = true;
            await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
            await SendSetupLinkIfRequestedAsync(registerDto.SendSetupLink, doctor, adminId);

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
                throw new NotFoundException("Doctor not found or access denied");

            if (updateDto.DepartmentId != doctor.DepartmentId)
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(updateDto.DepartmentId);
                if (department == null || department.FacultyId != admin.FacultyId)
                    throw new ForbiddenException("Department does not belong to your faculty");
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
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            var admin = await GetAdminWithFaculty(adminId);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Proctor"))
                .FirstOrDefault() ?? throw new NotFoundException("Proctor role not found");

            var proctor = _mapper.Map<Proctor>(registerDto);
            proctor.PasswordHash = HashPassword(registerDto.Password);
            proctor.MustChangePassword = true;
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
            await SendSetupLinkIfRequestedAsync(registerDto.SendSetupLink, proctor, adminId);

            await _audit.LogAsync(Entry(admin, ActivityActions.Create, ActivityEntities.Proctor, proctor.Id,
                $"Added proctor {proctor.FirstName} {proctor.LastName}", null, Snapshot(proctor, ActivityEntities.Proctor)));

            var createdProctor = await _unitOfWork.Repository<Proctor>()
                .GetByIdAsync(proctor.Id, p => p.Role);

            return _mapper.Map<ProctorDto>(createdProctor);
        }

        public async Task<List<ProctorDto>> GetFacultyProctorsAsync(int adminId, bool includeDisabled = false)
        {
            var admin = await GetAdminWithFaculty(adminId);

            var proctors = await _unitOfWork.Repository<Proctor>()
                .FindAsync(p => p.FacultyId == admin.FacultyId && (includeDisabled || p.IsActive), p => p.Role);

            return _mapper.Map<List<ProctorDto>>(proctors);
        }

        public async Task<PagedResult<StudentDto>> GetFacultyStudentsPagedAsync(int adminId, PageQuery q)
        {
            var admin = await GetAdminWithFaculty(adminId);
            var facultyId = admin.FacultyId;

            var query = _unitOfWork.Repository<Student>()
                .GetQueryable(s => s.University, s => s.Faculty, s => s.Department, s => s.Role)
                .Where(s => s.FacultyId == facultyId);

            if (q.Status == "active") query = query.Where(s => s.IsActive);
            else if (q.Status == "inactive") query = query.Where(s => !s.IsActive);

            if (q.DepartmentId is int dep) query = query.Where(s => s.DepartmentId == dep);
            if (q.Year is int yr) query = query.Where(s => (int)s.Year == yr);

            var term = q.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
                query = query.Where(s => s.FirstName.Contains(term) || s.LastName.Contains(term)
                    || (s.FirstName + " " + s.LastName).Contains(term)
                    || s.Email.Contains(term) || s.NationalId.Contains(term));

            var total = await query.CountAsync();

            IOrderedQueryable<Student> ordered = (q.Sort?.ToLowerInvariant(), q.Desc) switch
            {
                ("name", false) => query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName),
                ("name", true) => query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName),
                ("department", false) => query.OrderBy(s => s.Department.Name),
                ("department", true) => query.OrderByDescending(s => s.Department.Name),
                ("year", false) => query.OrderBy(s => s.Year),
                ("year", true) => query.OrderByDescending(s => s.Year),
                ("status", false) => query.OrderBy(s => s.IsActive),
                ("status", true) => query.OrderByDescending(s => s.IsActive),
                _ => query.OrderBy(s => s.Id)
            };

            var page = q.SafePage; var size = q.SafeSize;
            var items = await ordered.ThenBy(s => s.Id).Skip((page - 1) * size).Take(size).ToListAsync();

            return new PagedResult<StudentDto>
            {
                Items = _mapper.Map<List<StudentDto>>(items),
                Total = total, Page = page, PageSize = size
            };
        }

        public async Task<PagedResult<DoctorDto>> GetFacultyDoctorsPagedAsync(int adminId, PageQuery q)
        {
            var admin = await GetAdminWithFaculty(adminId);
            var facultyId = admin.FacultyId;

            var query = _unitOfWork.Repository<Doctor>()
                .GetQueryable(d => d.University, d => d.Faculty, d => d.Department, d => d.Role)
                .Where(d => d.FacultyId == facultyId);

            if (q.Status == "active") query = query.Where(d => d.IsActive);
            else if (q.Status == "inactive") query = query.Where(d => !d.IsActive);

            if (q.DepartmentId is int dep) query = query.Where(d => d.DepartmentId == dep);

            var term = q.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
                query = query.Where(d => d.FirstName.Contains(term) || d.LastName.Contains(term)
                    || (d.FirstName + " " + d.LastName).Contains(term)
                    || d.Email.Contains(term) || d.Specialization.Contains(term));

            var total = await query.CountAsync();

            IOrderedQueryable<Doctor> ordered = (q.Sort?.ToLowerInvariant(), q.Desc) switch
            {
                ("name", false) => query.OrderBy(d => d.FirstName).ThenBy(d => d.LastName),
                ("name", true) => query.OrderByDescending(d => d.FirstName).ThenByDescending(d => d.LastName),
                ("department", false) => query.OrderBy(d => d.Department.Name),
                ("department", true) => query.OrderByDescending(d => d.Department.Name),
                ("status", false) => query.OrderBy(d => d.IsActive),
                ("status", true) => query.OrderByDescending(d => d.IsActive),
                _ => query.OrderBy(d => d.Id)
            };

            var page = q.SafePage; var size = q.SafeSize;
            var items = await ordered.ThenBy(d => d.Id).Skip((page - 1) * size).Take(size).ToListAsync();

            return new PagedResult<DoctorDto>
            {
                Items = _mapper.Map<List<DoctorDto>>(items),
                Total = total, Page = page, PageSize = size
            };
        }

        public async Task<PagedResult<ProctorDto>> GetFacultyProctorsPagedAsync(int adminId, PageQuery q)
        {
            var admin = await GetAdminWithFaculty(adminId);
            var facultyId = admin.FacultyId;

            var query = _unitOfWork.Repository<Proctor>()
                .GetQueryable(p => p.Role)
                .Where(p => p.FacultyId == facultyId);

            if (q.Status == "active") query = query.Where(p => p.IsActive);
            else if (q.Status == "inactive") query = query.Where(p => !p.IsActive);

            var term = q.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
                query = query.Where(p => p.FirstName.Contains(term) || p.LastName.Contains(term)
                    || (p.FirstName + " " + p.LastName).Contains(term)
                    || p.Email.Contains(term) || p.Department.Contains(term));

            var total = await query.CountAsync();

            IOrderedQueryable<Proctor> ordered = (q.Sort?.ToLowerInvariant(), q.Desc) switch
            {
                ("name", false) => query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
                ("name", true) => query.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
                ("department", false) => query.OrderBy(p => p.Department),
                ("department", true) => query.OrderByDescending(p => p.Department),
                ("status", false) => query.OrderBy(p => p.IsActive),
                ("status", true) => query.OrderByDescending(p => p.IsActive),
                _ => query.OrderBy(p => p.Id)
            };

            var page = q.SafePage; var size = q.SafeSize;
            var items = await ordered.ThenBy(p => p.Id).Skip((page - 1) * size).Take(size).ToListAsync();

            return new PagedResult<ProctorDto>
            {
                Items = _mapper.Map<List<ProctorDto>>(items),
                Total = total, Page = page, PageSize = size
            };
        }

        public async Task<ProctorDto> UpdateProctorAsync(int proctorId, UpdateProctorDto updateDto, int adminId)
        {
            Validate(updateDto);

            var admin = await GetAdminWithFaculty(adminId);

            var proctor = await _unitOfWork.Repository<Proctor>()
                .GetByIdAsync(proctorId, p => p.Role);

            if (proctor == null || proctor.FacultyId != admin.FacultyId)
                throw new NotFoundException("Proctor not found or access denied");

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
            PrepareInitialPassword(dto);
            Validate(dto);
            PasswordPolicy.Enforce(dto.Password);

            var actor = await GetAdminWithFaculty(actorId);
            var actorRole = StaffRules.RoleOf(actor);

            if (dto.Role != AppRoles.Secretary && dto.Role != AppRoles.Technical)
                throw new BadRequestException("Invalid staff role");

            // منع التصعيد: الأمين يعيّن تيم فني بس، والعميد يعيّن أمين وتيم فني
            if (!StaffRules.CanManageStaff(actorRole, dto.Role))
                throw new ForbiddenException("You don't have permission to appoint this role");

            await EnsureUniqueAsync(dto.Email, dto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == dto.Role))
                .FirstOrDefault() ?? throw new NotFoundException($"Role '{dto.Role}' not found");

            Admin staff = dto.Role == AppRoles.Secretary ? new FacultySecretary() : new TechnicalStaff();
            staff.FirstName = dto.FirstName;
            staff.LastName = dto.LastName;
            staff.Email = dto.Email;
            staff.PasswordHash = HashPassword(dto.Password);
            staff.MustChangePassword = true;
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
            await SendSetupLinkIfRequestedAsync(dto.SendSetupLink, staff, actorId);

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
                throw new NotFoundException("Staff member not found or access denied");

            if (!StaffRules.CanManageStaff(StaffRules.RoleOf(actor), StaffRules.RoleOf(staff)))
                throw new ForbiddenException("You don't have permission to manage this staff member");

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
                throw new ForbiddenException("Only the dean or the faculty secretary can undo actions");

            var log = await _audit.GetByIdAsync(logId) ?? throw new NotFoundException("Log entry not found");

            if (log.FacultyId != actor.FacultyId)
                throw new NotFoundException("Log entry not found or access denied");
            if (log.IsReverted)
                throw new BadRequestException("This action was already undone");
            if (log.EntityId == null || !Tracked.ContainsKey(log.EntityType)
                || !ActivityActions.IsReversible(log.EntityType, log.Action))
                throw new BadRequestException("This action can't be undone here");

            // _db.Users مفيهوش فلتر الحذف، فبنقدر نوصل للمحذوفين
            var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == log.EntityId.Value)
                ?? throw new NotFoundException("The record no longer exists");

            var typeOk = log.EntityType switch
            {
                ActivityEntities.Student => target is Student,
                ActivityEntities.Doctor => target is Doctor,
                ActivityEntities.Proctor => target is Proctor,
                ActivityEntities.Staff => target is FacultySecretary || target is TechnicalStaff,
                _ => false
            };
            if (!typeOk || FacultyOf(target) != actor.FacultyId)
                throw new NotFoundException("Record not found or access denied");

            if (log.EntityType == ActivityEntities.Staff && !StaffRules.CanManageStaff(role, StaffRules.RoleOf(target)))
                throw new ForbiddenException("You don't have permission to manage this staff member");

            var now = DateTime.UtcNow;
            string summary;

            switch (log.Action)
            {
                case ActivityActions.Create:
                case ActivityActions.Restore:
                    if (target.IsDeleted) throw new BadRequestException("The record is already deleted");
                    target.IsDeleted = true;
                    target.DeletedAt = now;
                    target.DeletedBy = actor.Id;
                    summary = "Undid creation (record removed)";
                    break;

                case ActivityActions.Delete:
                    if (!target.IsDeleted) throw new BadRequestException("The record is already restored");
                    target.IsDeleted = false;
                    target.DeletedAt = null;
                    target.DeletedBy = null;
                    summary = "Restored deleted record";
                    break;

                default: // Update / Disable / Enable
                    if (target.IsDeleted) throw new BadRequestException("The record is deleted. Undo the delete first");
                    if (string.IsNullOrEmpty(log.OldValues)) throw new BadRequestException("No previous values were recorded");
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
            Validate(changePasswordDto);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            if (!user.IsActive)
                throw new ForbiddenException("Account is disabled", "AUTH_DISABLED");

            if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                throw new BadRequestException("Current password is incorrect");

            if (changePasswordDto.NewPassword == changePasswordDto.CurrentPassword)
                throw new BadRequestException("The new password must be different from the current one");

            PasswordPolicy.Enforce(changePasswordDto.NewPassword);

            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Changing the password signs the user out of all devices
            var open = _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null);
            await open.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);

            await _db.UserSessions.Where(s => s.UserId == userId && s.EndedAt == null)
                .ForEachAsync(s => s.EndedAt = DateTime.UtcNow);

            await _db.SaveChangesAsync();

            return true;
        }

        private const int ResetTokenMinutes = 30;

        /// <summary>يتحقق إن الأدمن مسموحله يعيد ضبط الحساب ده، ويرجّع الحساب ودور الأدمن.</summary>
        private async Task<(User Target, string ActorRole)> ResolveResetTargetAsync(int targetId, int actorId)
        {
            var actor = await _unitOfWork.Repository<User>().GetByIdAsync(actorId);
            if (actor == null || !actor.IsActive)
                throw new ForbiddenException("Your account is disabled");

            var actorRole = StaffRules.RoleOf(actor);
            if (actorRole is not (AppRoles.SuperAdmin or AppRoles.Dean or AppRoles.Secretary or AppRoles.Technical))
                throw new ForbiddenException();

            if (targetId == actorId)
                throw new BadRequestException("Use the change password page for your own account");

            var target = await _unitOfWork.Repository<User>().GetByIdAsync(targetId)
                ?? throw new NotFoundException("User not found");

            var targetRole = StaffRules.RoleOf(target);
            var isSuper = actorRole == AppRoles.SuperAdmin;
            var actorFaculty = ChatRules.FacultyOf(actor);

            // خارج النطاق = "مش موجود" عشان منسرّبش وجود حسابات كليات تانية
            if (!isSuper && ChatRules.FacultyOf(target) != actorFaculty)
                throw new NotFoundException("User not found");

            bool allowed = target switch
            {
                Admin => StaffRules.CanManageStaff(actorRole, targetRole),
                Support => isSuper,
                Student => !isSuper,
                Doctor or Proctor => actorRole is AppRoles.Dean or AppRoles.Secretary,
                _ => false
            };
            if (!allowed)
                throw new ForbiddenException("You don't have permission to reset this account's password");

            return (target, actorRole);
        }

        /// <summary>البريد مخفي اللي هيتبعت له الرابط، عشان الأدمن يتأكد قبل الإرسال.</summary>
        public async Task<string> PreviewResetEmailAsync(int targetId, int actorId)
        {
            var (target, _) = await ResolveResetTargetAsync(targetId, actorId);
            return MaskEmail(target.Email);
        }

        /// <summary>يبعت رابط إعادة التعيين لبريد الحساب. مابيرجعش أي باسورد، وبيرجّع البريد مخفي.</summary>
        public async Task<string> ResetPasswordAsync(int targetId, int actorId)
        {
            var (target, actorRole) = await ResolveResetTargetAsync(targetId, actorId);
            var targetRole = StaffRules.RoleOf(target);
            var now = DateTime.UtcNow;

            // نلغي كل الجلسات والروابط القديمة للحساب
            await _db.RefreshTokens.Where(t => t.UserId == targetId && t.RevokedAt == null)
                .ForEachAsync(t => t.RevokedAt = now);
            await _db.UserSessions.Where(s => s.UserId == targetId && s.EndedAt == null)
                .ForEachAsync(s => s.EndedAt = now);
            await _db.PasswordResetTokens.Where(t => t.UserId == targetId && t.UsedAt == null)
                .ForEachAsync(t => t.UsedAt = now);

            // ونفك القفل لو الحساب اتقفل من محاولات غلط
            var email = target.Email.Trim().ToLowerInvariant();
            var nid = target.NationalId.Trim().ToLowerInvariant();
            var locks = await _db.LoginLockouts
                .Where(l => l.Key.StartsWith($"login:{email}:") || l.Key == $"acct:{email}"
                         || l.Key.StartsWith($"login:{nid}:") || l.Key == $"acct:{nid}")
                .ToListAsync();
            _db.LoginLockouts.RemoveRange(locks);

            await IssueSetupLinkAsync(target, actorId, isNewAccount: false);

            var faculty = ChatRules.FacultyOf(target);
            _audit.Stage(new AuditEntry
            {
                ActorId = actorId,
                ActorRole = actorRole,
                FacultyId = faculty > 0 ? faculty : null,
                Action = ActivityActions.ResetPassword,
                EntityType = targetRole switch
                {
                    AppRoles.Student => ActivityEntities.Student,
                    AppRoles.Doctor => ActivityEntities.Doctor,
                    AppRoles.Proctor => ActivityEntities.Proctor,
                    _ => ActivityEntities.Staff
                },
                EntityId = target.Id,
                Description = $"Sent a password reset link to {targetRole} {target.FirstName} {target.LastName}"
            });
            await _unitOfWork.SaveChangesAsync();

            return MaskEmail(target.Email);
        }

        /// <summary>يتحقق من الرابط ويعيّن الباسورد الجديد. الرابط يتستخدم مرة واحدة وبمدة محدودة.</summary>
        public async Task ConfirmPasswordResetAsync(string token, string newPassword)
        {
            const string invalid = "This reset link is invalid or has expired. Ask your administrator for a new one.";
            if (string.IsNullOrWhiteSpace(token) || token.Length != 64)
                throw new BadRequestException(invalid);

            PasswordPolicy.Enforce(newPassword);

            var now = DateTime.UtcNow;
            var hash = HashToken(token);
            var record = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (record == null || record.UsedAt != null || record.ExpiresAt < now)
                throw new BadRequestException(invalid);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == record.UserId);
            if (user == null || user.IsDeleted || !user.IsActive)
                throw new BadRequestException(invalid);

            if (VerifyPassword(newPassword, user.PasswordHash))
                throw new BadRequestException("The new password must be different from the old one");

            user.PasswordHash = HashPassword(newPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = now;
            record.UsedAt = now;

            // أي جلسة قديمة تتقفل بعد تغيير الباسورد
            await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ForEachAsync(t => t.RevokedAt = now);
            await _db.UserSessions.Where(s => s.UserId == user.Id && s.EndedAt == null)
                .ForEachAsync(s => s.EndedAt = now);

            await _db.SaveChangesAsync();
        }

        // Login audit rows keep only a hint of the account, never the full email or national ID
        private static string MaskAccount(string? account)
        {
            if (string.IsNullOrWhiteSpace(account)) return "unknown";
            if (account.Contains('@')) return MaskEmail(account);
            return account.Length <= 4 ? "****" : "***" + account[^4..];
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            return at < 1 ? "***" : $"{email[0]}***{email[at..]}";
        }

        private string ResetPageUrl() =>
            _configuration["Frontend:ResetPasswordUrl"] ?? "http://localhost:3000/reset-password";

        #endregion

        #region Support Management (SuperAdmin only)

        public async Task<SupportDto> RegisterSupportAsync(RegisterSupportDto registerDto, int actorId)
        {
            PrepareInitialPassword(registerDto);
            Validate(registerDto);
            PasswordPolicy.Enforce(registerDto.Password);

            await EnsureUniqueAsync(registerDto.Email, registerDto.NationalId);

            var role = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Support"))
                .FirstOrDefault() ?? throw new NotFoundException("Support role not found");

            var support = _mapper.Map<Support>(registerDto);
            support.PasswordHash = HashPassword(registerDto.Password);
            support.MustChangePassword = true;
            support.RoleId = role.Id;
            support.ProfileImage = "default.jpg";
            support.CreatedAt = DateTime.UtcNow;
            support.IsActive = true;
            await _unitOfWork.Repository<Support>().AddAsync(support);
            await _unitOfWork.SaveChangesAsync();
            await SendSetupLinkIfRequestedAsync(registerDto.SendSetupLink, support, actorId);

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
                throw new NotFoundException("Support not found");

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
                throw new NotFoundException("Support not found");

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
            Validate(updateDto);

            var university = await _unitOfWork.Repository<University>().GetByIdAsync(updateDto.UniversityId);
            if (university == null)
                throw new NotFoundException("University not found");

            var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(updateDto.FacultyId);
            if (faculty == null)
                throw new NotFoundException("Faculty not found");

            if (faculty.UniversityId != university.Id)
                throw new BadRequestException("Faculty does not belong to the specified university");

            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId,
                    a => a.University,
                    a => a.Faculty,
                    a => a.Role);

            if (admin == null || admin is FacultySecretary || admin is TechnicalStaff)
                throw new NotFoundException("Admin not found");

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

        public async Task<bool> DeleteAdminAsync(int adminId, int actorId = 0)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId);

            if (admin == null || admin is FacultySecretary || admin is TechnicalStaff)
                throw new NotFoundException("Admin not found");

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

        /// <summary>
        /// لو طُلب رابط الإعداد، الحساب بيتعمل بكلمة مرور عشوائية محدش يعرفها، والمستخدم يختار كلمته من الإيميل.
        /// غير كده لازم كلمة مرور.
        /// </summary>
        private static void PrepareInitialPassword(RegisterUserDto dto)
        {
            if (dto.SendSetupLink)
            {
                dto.Password = "Aa1" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                return;
            }
            if (string.IsNullOrEmpty(dto.Password))
                throw new BadRequestException("Password is required");
        }

        /// <summary>يبعت رابط الإعداد لحساب جديد. لو الإيميل فشل الحساب يفضل موجود والأدمن يقدر يعيد الإرسال من زر إعادة التعيين.</summary>
        private async Task SendSetupLinkIfRequestedAsync(bool requested, User created, int requestedBy)
        {
            if (!requested) return;
            try { await IssueSetupLinkAsync(created, requestedBy, isNewAccount: true); }
            catch (AppException) { }
        }

        /// <summary>يصدر رابط ضبط كلمة المرور لحساب ويبعته بالإيميل. الصلاحية لازم تكون اتفحصت قبل النداء.</summary>
        private async Task IssueSetupLinkAsync(User target, int requestedBy, bool isNewAccount)
        {
            var now = DateTime.UtcNow;
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var resetRow = new PasswordResetToken
            {
                UserId = target.Id,
                TokenHash = HashToken(rawToken),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(ResetTokenMinutes),
                RequestedBy = requestedBy
            };
            _db.PasswordResetTokens.Add(resetRow);
            await _unitOfWork.SaveChangesAsync();

            var link = $"{ResetPageUrl()}?token={rawToken}";
            var intro = isNewAccount
                ? "<p>Your TruthEye account has been created. Use the link below to choose your password. "
                : "<p>An administrator asked for a password reset on your account. Use the link below to choose a new password. ";
            try
            {
                await _email.SendAsync(target.Email,
                    isNewAccount ? "Set your TruthEye password" : "Reset your TruthEye password",
                    $"<p>Hello {System.Net.WebUtility.HtmlEncode(target.FirstName)},</p>" +
                    intro + $"The link works once and expires in {ResetTokenMinutes} minutes.</p>" +
                    $"<p><a href=\"{link}\">{(isNewAccount ? "Set password" : "Reset password")}</a></p>" +
                    "<p>If you did not expect this, contact your faculty administration.</p>");
            }
            catch (Exception)
            {
                // الرابط ماوصلش: نبطله عشان ماييجيش شغال من غير ما حد يعرفه
                resetRow.UsedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                throw new BadRequestException("Couldn't send the email. Check the email settings and try again.");
            }
        }

        private static void Validate(object dto)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
            {
                var errors = string.Join(", ", results.Select(v => v.ErrorMessage));
                throw new BadRequestException($"Validation failed: {errors}");
            }
        }

        /// <summary>
        /// بنفحص جدول Users مباشرة (مش الـ Repository) عشان المحذوفين كمان،
        /// لأن الـ unique index على الإيميل والرقم القومي لسه حاجزهم.
        /// </summary>
        private async Task EnsureUniqueAsync(string email, string nationalId)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email || u.NationalId == nationalId))
                throw new ConflictException("User with this email or national ID already exists");
        }

        private async Task<Admin> GetAdminWithFaculty(int adminId)
        {
            var admin = await _unitOfWork.Repository<Admin>()
                .GetByIdAsync(adminId, a => a.Faculty);

            if (admin == null)
                throw new ForbiddenException("Account not found");

            if (!admin.IsActive)
                throw new ForbiddenException("Your account is disabled");

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
                    if (!target.IsActive) throw new BadRequestException("Already disabled");
                    target.IsActive = false;
                    break;
                case ActivityActions.Enable:
                    if (target.IsActive) throw new BadRequestException("Already active");
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
                throw new ForbiddenException("You don't have permission to delete. You can disable the account instead.");

            var target = await _unitOfWork.Repository<T>().GetByIdAsync(id);
            if (target == null || FacultyOf(target) != actor.FacultyId)
                throw new NotFoundException($"{entityType} not found or access denied");

            var before = Snapshot(target, entityType);
            ApplyState(target, action, actor.Id);

            _audit.Stage(Entry(actor, action, entityType, target.Id,
                $"{action} {entityType.ToLower()} {target.FirstName} {target.LastName}", before,
                action == ActivityActions.Delete ? null : Snapshot(target, entityType)));

            await _unitOfWork.Repository<T>().UpdateAsync(target);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task LogLoginAsync(string action, User? user, string account, string ip, string text)
        {
            var faculty = user == null ? 0 : ChatRules.FacultyOf(user);
            await _audit.LogAsync(new AuditEntry
            {
                ActorId = user?.Id,
                ActorRole = user == null ? string.Empty : StaffRules.RoleOf(user),
                FacultyId = faculty > 0 ? faculty : null,
                Action = action,
                EntityType = ActivityEntities.Login,
                EntityId = user?.Id,
                Description = $"{text} ({MaskAccount(account)}) from {ip}",
                IpAddress = ip
            });
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

        // الـ sid هنا مش بيتخزن، بس التوقيع متوافق مع باقي الاستدعاءات
        private async Task<string> IssueRefreshAsync(int userId, string? sid = null)
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
            if (stored == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            if (stored.RevokedAt != null)
            {
                // A token that was already rotated/revoked is being replayed. Two tabs can refresh at the same
                // moment, so only treat it as theft after a short grace window; then cut every token of that user.
                if (stored.RevokedAt < DateTime.UtcNow.AddSeconds(-30))
                {
                    var now = DateTime.UtcNow;
                    await _db.RefreshTokens
                        .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                        .ForEachAsync(t => t.RevokedAt = now);
                    await _db.SaveChangesAsync();
                }
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            if (stored.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid refresh token");

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(stored.UserId, u => u.Role);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled");

            var session = await _db.UserSessions
                .Where(s => s.UserId == user.Id && s.EndedAt == null)
                .OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
            if (session == null)
                throw new UnauthorizedAccessException("Session ended");   // اتقفلت (دخول من جهاز تاني بعد انتهاء النبض أو Logout)
            session.LastSeenAt = DateTime.UtcNow;

            stored.RevokedAt = DateTime.UtcNow;   // rotation: the old one dies, saved by IssueRefreshAsync
            return new TokenPair(GenerateJwtToken(user, session.Sid), await IssueRefreshAsync(user.Id, session.Sid), user.MustChangePassword);
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

        public async Task EndSessionAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;
            var hash = HashToken(refreshToken);
            var stored = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored == null) return;

            foreach (var s in await _db.UserSessions.Where(s => s.UserId == stored.UserId && s.EndedAt == null).ToListAsync())
                s.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private string GenerateJwtToken(User user, string? sid = null)
        {
            var userType = StaffRules.RoleOf(user);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, userType),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("te_sid", sid ?? "")
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
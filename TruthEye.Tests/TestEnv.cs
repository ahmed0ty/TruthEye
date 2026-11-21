using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TruthEye.Data;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Mapping;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Tests;

/// كلية "أ" وكلية "ب"، لكل واحدة عميد، وطالب، ومراقب. الأدوار (1..8) جاية من الـ seed.
public class TestEnv : IDisposable
{
    public AppDbContext Db { get; }
    public AuthService Auth { get; }
    public CourseService Courses { get; }
    public FakeEmailSender Mail { get; }

    public Faculty FacA { get; }
    public Faculty FacB { get; }
    public Department DeptA { get; }
    public Department DeptB { get; }

    public Admin DeanA { get; }
    public Admin DeanB { get; }
    public FacultySecretary SecretaryA { get; }
    public TechnicalStaff TechA { get; }
    public FacultySecretary SecretaryB { get; }

    public Student StudentA { get; }
    public Student StudentB { get; }
    public Doctor DoctorA { get; }
    public Proctor ProctorA { get; }
    public Proctor ProctorB { get; }

    private int _counter;

    public const int RoleStudent = 1, RoleDoctor = 2, RoleAdmin = 3, RoleProctor = 4, RoleSecretary = 7, RoleTechnical = 8;

    public TestEnv(IEmailSender? sender = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();

        var uni = new University { Name = "Uni", City = "C", Country = "EG", Location = "L", CreatedAt = DateTime.UtcNow };
        Db.Universities.Add(uni);
        Db.SaveChanges();

        FacA = new Faculty { Name = "Faculty A", UniversityId = uni.Id };
        FacB = new Faculty { Name = "Faculty B", UniversityId = uni.Id };
        Db.Faculties.AddRange(FacA, FacB);
        Db.SaveChanges();

        DeptA = new Department { Name = "Dept A", Description = "d", FacultyId = FacA.Id };
        DeptB = new Department { Name = "Dept B", Description = "d", FacultyId = FacB.Id };
        Db.Departments.AddRange(DeptA, DeptB);
        Db.SaveChanges();

        DeanA = Person<Admin>("deanA", RoleAdmin, a => { a.UniversityId = uni.Id; a.FacultyId = FacA.Id; });
        DeanB = Person<Admin>("deanB", RoleAdmin, a => { a.UniversityId = uni.Id; a.FacultyId = FacB.Id; });
        SecretaryA = Person<FacultySecretary>("secA", RoleSecretary, a => { a.UniversityId = uni.Id; a.FacultyId = FacA.Id; });
        TechA = Person<TechnicalStaff>("techA", RoleTechnical, a => { a.UniversityId = uni.Id; a.FacultyId = FacA.Id; });
        SecretaryB = Person<FacultySecretary>("secB", RoleSecretary, a => { a.UniversityId = uni.Id; a.FacultyId = FacB.Id; });

        StudentA = Person<Student>("stuA", RoleStudent, s => { s.UniversityId = uni.Id; s.FacultyId = FacA.Id; s.DepartmentId = DeptA.Id; });
        StudentB = Person<Student>("stuB", RoleStudent, s => { s.UniversityId = uni.Id; s.FacultyId = FacB.Id; s.DepartmentId = DeptB.Id; });
        DoctorA = Person<Doctor>("docA", RoleDoctor, d => { d.UniversityId = uni.Id; d.FacultyId = FacA.Id; d.DepartmentId = DeptA.Id; d.Specialization = "S"; });
        ProctorA = Person<Proctor>("prA", RoleProctor, p => { p.Department = "X"; p.FacultyId = FacA.Id; });
        ProctorB = Person<Proctor>("prB", RoleProctor, p => { p.Department = "X"; p.FacultyId = FacB.Id; });

        var mapperServices = new ServiceCollection();
        mapperServices.AddLogging();
        mapperServices.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
        var mapper = mapperServices.BuildServiceProvider().GetRequiredService<IMapper>();
        var audit = new SystemLogService(Db, new HttpContextAccessor(), NullLogger<SystemLogService>.Instance);
        var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Key"] = "test-key-test-key-test-key-test-key-1234567890",
        ["Jwt:Issuer"] = "test",
        ["Jwt:Audience"] = "test",
        ["Jwt:AccessMinutes"] = "15"
    })
    .Build();
        Mail = new FakeEmailSender();
        Auth = new AuthService(new UnitOfWork(Db), mapper, config, Db, audit, sender ?? Mail);

        // من غير connection string للـ Redis، الـ CacheService بيشتغل على الـ memory
        var cache = new CacheService(
            new MemoryCache(new MemoryCacheOptions()),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<CacheService>.Instance,
            config);

        Courses = new CourseService(new UnitOfWork(Db), mapper, NullLogger<CourseService>.Instance,
            cache, new FakeNotifications(), Db, audit);
    }

    private T Person<T>(string tag, int roleId, Action<T> setup) where T : User, new()
    {
        var u = new T
        {
            FirstName = tag,
            LastName = "Test",
            Email = $"{tag}@test.com",
            PasswordHash = "x",
            NationalId = (30000000000000L + ++_counter).ToString(),
            PhoneNumber = "01000000000",
            ProfileImage = "default.jpg",
            IsActive = true,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow
        };
        setup(u);
        Db.Add(u);
        Db.SaveChanges();
        return u;
    }

    public Student ExtraStudent(string tag, bool deleted = false)
    {
        var s = Person<Student>(tag, RoleStudent, x =>
        {
            x.UniversityId = StudentA.UniversityId;
            x.FacultyId = FacA.Id;
            x.DepartmentId = DeptA.Id;
            x.IsDeleted = deleted;
        });
        return s;
    }

    public void Dispose() => Db.Dispose();
}

/// بريد وهمي: بيسجّل الرسايل بدل ما يبعتها.
public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

/// إيميل بيفشل دايمًا، عشان نختبر إن الحساب بيتعمل والرابط بيتبطل.
public class FailingEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody) =>
        throw new InvalidOperationException("SMTP down");
}

/// بديل للإشعارات في الاختبارات: مابيبعتش حاجة فعلًا.
public class FakeNotifications : INotificationService
{
    public Task SendNotificationAsync(int userId, string message, string type) => Task.CompletedTask;

    public Task SendBulkNotificationAsync(string role, string message, string type) => Task.CompletedTask;

    public Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId) =>
        Task.FromResult<IEnumerable<Notification>>(new List<Notification>());

    public Task MarkAsReadAsync(int notificationId, int userId) => Task.CompletedTask;
}
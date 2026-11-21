using Microsoft.Extensions.Caching.Memory;
using TruthEye.Entities;
using TruthEye.Services;

namespace TruthEye.Tests;

public class ChatLockTests
{
    [Fact]
    public async Task Chat_is_locked_only_for_the_faculty_with_a_running_exam()
    {
        using var env = new TestEnv();
        var course = new Course { Name = "C", Description = "d", DepartmentId = env.DeptA.Id };
        env.Db.Courses.Add(course);
        env.Db.SaveChanges();

        var svc = new ChatLockService(env.Db, new MemoryCache(new MemoryCacheOptions()));
        Assert.False((await svc.GetAsync(env.FacA.Id)).Locked);

        env.Db.Exams.Add(new Exam
{
    Title = "E", Instructions = "i", Type = "Online", Status = "Active",
    CourseId = course.Id, DoctorId = env.DoctorA.Id,
    Course = course,
    Doctor = env.DoctorA,
    Questions = new List<Question>(),
    ExamSessions = new List<ExamSession>(),
    Proctors = new List<Proctor>(),
    StartTime = DateTime.UtcNow.AddMinutes(-10), EndTime = DateTime.UtcNow.AddMinutes(50),
    Duration = 60, MaxAttempts = 1, TotalMarks = 100
});
        env.Db.SaveChanges();

        // كاش جديد عشان نتفادى نتيجة الاستعلام الأول
        svc = new ChatLockService(env.Db, new MemoryCache(new MemoryCacheOptions()));
        Assert.True((await svc.GetAsync(env.FacA.Id)).Locked);
        Assert.False((await svc.GetAsync(env.FacB.Id)).Locked);
    }
}
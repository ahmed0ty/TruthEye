using Microsoft.EntityFrameworkCore;
using TruthEye.Entities;
using TruthEye.Services;

namespace TruthEye.Tests;

public class AnalyticsTests
{
    [Fact]
    public void Buckets_put_100_in_the_last_bucket_and_clamp_values()
    {
        var b = AnalyticsService.Bucketize(new[] { 0.0, 9.9, 10, 55, 100, 120, -5 });
        Assert.Equal(10, b.Count);
        Assert.Equal(3, b[0].Count);   // 0, 9.9, -5
        Assert.Equal(1, b[1].Count);
        Assert.Equal(1, b[5].Count);
        Assert.Equal(2, b[9].Count);   // 100, 120
    }

    [Fact]
    public async Task Faculty_analytics_only_counts_own_faculty_and_exam_analytics_only_own_exams()
    {
        using var env = new TestEnv();
        var course = new Course
        {
            Name = "C", Description = "d", DepartmentId = env.DeptA.Id, Department = env.DeptA,
            Instructors = new List<Doctor>(), RegisteredStudents = new List<Student>(), Exams = new List<Exam>()
        };
        env.Db.Courses.Add(course);
        env.Db.SaveChanges();

        var now = DateTime.UtcNow;
        var exam = new Exam
        {
            Title = "E", Instructions = "i", Type = "Online", Status = "Active",
            CourseId = course.Id, DoctorId = env.DoctorA.Id, Course = course, Doctor = env.DoctorA,
            Questions = new List<Question>(), ExamSessions = new List<ExamSession>(), Proctors = new List<Proctor>(),
            StartTime = now.AddHours(-3), EndTime = now.AddHours(-1), Duration = 60, MaxAttempts = 1, TotalMarks = 10, PassingScore = 50
        };
        env.Db.Exams.Add(exam);
        env.Db.SaveChanges();

        env.Db.ExamSessions.Add(new ExamSession
        {
            Status = "Completed", StudentId = env.StudentA.Id, ExamId = exam.Id, StartTime = now.AddHours(-2),
            TotalMarks = 10, TotalScore = 8, Percentage = 80, IsPassed = true
        });
        env.Db.SaveChanges();

        var svc = new AnalyticsService(env.Db);

        var a = await svc.FacultyAsync(env.DeanA.Id, now.AddDays(-1), now);
        Assert.Equal(1, a.TotalSessions);
        Assert.Equal(80, a.AvgPercentage);

        var b = await svc.FacultyAsync(env.DeanB.Id, now.AddDays(-1), now);
        Assert.Equal(0, b.TotalSessions);

        var ex = await svc.ExamAsync(env.DoctorA.Id, exam.Id);
        Assert.Equal(1, ex.Students);
        Assert.Equal(100, ex.PassRate);

        await Assert.ThrowsAnyAsync<Exception>(() => svc.ExamAsync(env.DeanA.Id, exam.Id));   // مش دكتور
    }
}
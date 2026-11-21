using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Services;

namespace TruthEye.Tests;

public class ResumeTests
{
    private static (ResumeService Svc, ExamSession Session) Build(TestEnv env, string reason = "lost-contact", int endedMinutesAgo = 2)
    {
        var course = new Course
        {
            Name = "C", Description = "d", DepartmentId = env.DeptA.Id, Department = env.DeptA,
            Instructors = new List<Doctor>(), RegisteredStudents = new List<Student>(), Exams = new List<Exam>()
        };
        env.Db.Courses.Add(course); env.Db.SaveChanges();

        var now = DateTime.UtcNow;
        var exam = new Exam
        {
            Title = "E", Instructions = "i", Type = "Online", Status = "Active",
            CourseId = course.Id, DoctorId = env.DoctorA.Id, Course = course, Doctor = env.DoctorA,
            Questions = new List<Question>(), ExamSessions = new List<ExamSession>(), Proctors = new List<Proctor>(),
            StartTime = now.AddMinutes(-20), EndTime = now.AddMinutes(60), Duration = 90, MaxAttempts = 1, TotalMarks = 10, PassingScore = 50
        };
        env.Db.Exams.Add(exam); env.Db.SaveChanges();

        var session = new ExamSession
        {
            Status = "Terminated", EndReason = reason, StudentId = env.StudentA.Id, ExamId = exam.Id,
            StartTime = now.AddMinutes(-15), EndTime = now.AddMinutes(-endedMinutesAgo), TotalMarks = 10
        };
        env.Db.ExamSessions.Add(session); env.Db.SaveChanges();

        var audit = new SystemLogService(env.Db, new HttpContextAccessor(), NullLogger<SystemLogService>.Instance);
        return (new ResumeService(env.Db, audit, new FakeNotifications(), NullLogger<ResumeService>.Instance), session);
    }

    [Fact]
    public async Task Student_asks_once_and_the_exam_doctor_approves_and_the_session_reopens()
    {
        using var env = new TestEnv();
        var (svc, s) = Build(env);

        var st = await svc.StatusAsync(env.StudentA.Id, s.Id);
        Assert.True(st.CanRequest);

        await svc.RequestAsync(env.StudentA.Id, new CreateResumeDto { ExamSessionId = s.Id, Reason = "Power cut" });
        await Assert.ThrowsAnyAsync<Exception>(() => svc.RequestAsync(env.StudentA.Id, new CreateResumeDto { ExamSessionId = s.Id, Reason = "Again please" }));

        var pending = await svc.PendingAsync(env.DoctorA.Id);
        Assert.Single(pending);

        await svc.DecideAsync(env.DoctorA.Id, pending[0].Id, new ResumeDecisionDto { Decision = "Approved" });

        var reopened = await env.Db.ExamSessions.SingleAsync(x => x.Id == s.Id);
        Assert.Equal("InProgress", reopened.Status);
        Assert.Contains(await env.Db.AILogs.ToListAsync(), l => l.EventType == "ResumedAfterDisconnect");
    }

    [Fact]
    public async Task Voluntary_exits_and_late_requests_cannot_be_resumed()
    {
        using var env = new TestEnv();
        var (svc, s) = Build(env, reason: "tab-hidden");
        await Assert.ThrowsAnyAsync<Exception>(() => svc.RequestAsync(env.StudentA.Id, new CreateResumeDto { ExamSessionId = s.Id, Reason = "Please let me" }));

        using var env2 = new TestEnv();
        var (svc2, s2) = Build(env2, endedMinutesAgo: 40);
        await Assert.ThrowsAnyAsync<Exception>(() => svc2.RequestAsync(env2.StudentA.Id, new CreateResumeDto { ExamSessionId = s2.Id, Reason = "Network was down" }));
    }

    [Fact]
    public async Task Only_the_exam_doctor_or_a_same_faculty_proctor_can_decide()
    {
        using var env = new TestEnv();
        var (svc, s) = Build(env);
        await svc.RequestAsync(env.StudentA.Id, new CreateResumeDto { ExamSessionId = s.Id, Reason = "Power cut" });
        var id = (await svc.PendingAsync(env.DoctorA.Id))[0].Id;

        await Assert.ThrowsAnyAsync<Exception>(() => svc.DecideAsync(env.DeanA.Id, id, new ResumeDecisionDto { Decision = "Approved" }));
        await Assert.ThrowsAnyAsync<Exception>(() => svc.DecideAsync(env.ProctorB.Id, id, new ResumeDecisionDto { Decision = "Approved" }));
        await svc.DecideAsync(env.ProctorA.Id, id, new ResumeDecisionDto { Decision = "Approved" });
    }
}
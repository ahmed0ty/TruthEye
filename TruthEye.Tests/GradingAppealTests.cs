using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Services;

namespace TruthEye.Tests;

public class GradingAppealTests
{
    private static (GradingService Grading, AppealService Appeals, ExamSession Session, Answer Answer) Build(TestEnv env, bool examEnded = true)
    {
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
            StartTime = now.AddHours(-3),
            EndTime = examEnded ? now.AddHours(-1) : now.AddHours(1),
            Duration = 60, MaxAttempts = 1, TotalMarks = 10, PassingScore = 50
        };
        env.Db.Exams.Add(exam);
        env.Db.SaveChanges();

        var question = new WrittenQuestion
{
    QuestionText = "Q?", Marks = 10, ExamId = exam.Id, Exam = exam,
    QuestionType = "Written",
    ModelAnswer = "m", AIThreshold = 0.7, EvaluationModel = "Default"
};
        env.Db.Questions.Add(question);
        env.Db.SaveChanges();

        var session = new ExamSession
        {
            Status = "Completed", StudentId = env.StudentA.Id, ExamId = exam.Id,
            StartTime = now.AddHours(-2), EndTime = now.AddHours(-1.5),
            TotalMarks = 10, TotalScore = 2, Percentage = 20, IsPassed = false
        };
        env.Db.ExamSessions.Add(session);
        env.Db.SaveChanges();

        var answer = new Answer
        {
            ExamSessionId = session.Id, QuestionId = question.Id,
            AnswerText = "a", MarksObtained = 2, AnsweredAt = now.AddHours(-2)
        };
        env.Db.Answers.Add(answer);
        env.Db.SaveChanges();

        var audit = new SystemLogService(env.Db, new HttpContextAccessor(), NullLogger<SystemLogService>.Instance);
        return (
            new GradingService(env.Db, audit, new FakeNotifications(), NullLogger<GradingService>.Instance),
            new AppealService(env.Db, audit, new FakeNotifications(), NullLogger<AppealService>.Instance),
            session, answer);
    }

    [Fact]
    public async Task Doctor_adjusting_a_grade_recalculates_the_session_and_logs_it()
    {
        using var env = new TestEnv();
        var (grading, _, session, answer) = Build(env);

        var result = await grading.AdjustAsync(env.DoctorA.Id, answer.Id, new AdjustGradeDto { Marks = 8, Reason = "Correct answer" });

        Assert.Equal(8, result.TotalScore);
        Assert.Equal(80, result.Percentage);
        Assert.True(result.IsPassed);
        Assert.Single(await env.Db.GradeAdjustments.ToListAsync());
        Assert.Contains(await env.Db.SystemLogs.ToListAsync(),
            l => l.Action == ActivityActions.GradeAdjusted && l.FacultyId == env.FacA.Id);
    }

    [Fact]
    public async Task Invalid_or_unauthorized_grade_changes_are_rejected()
    {
        using var env = new TestEnv();
        var (grading, _, _, answer) = Build(env);

        await Assert.ThrowsAnyAsync<Exception>(() => grading.AdjustAsync(env.DoctorA.Id, answer.Id, new AdjustGradeDto { Marks = 11, Reason = "too high" }));
        await Assert.ThrowsAnyAsync<Exception>(() => grading.AdjustAsync(env.DoctorA.Id, answer.Id, new AdjustGradeDto { Marks = -1, Reason = "negative" }));
        await Assert.ThrowsAnyAsync<Exception>(() => grading.AdjustAsync(env.DoctorA.Id, answer.Id, new AdjustGradeDto { Marks = 5, Reason = "" }));
        await Assert.ThrowsAnyAsync<Exception>(() => grading.AdjustAsync(env.DeanA.Id, answer.Id, new AdjustGradeDto { Marks = 5, Reason = "not a doctor" }));

        Assert.Equal(2, (await env.Db.Answers.SingleAsync(a => a.Id == answer.Id)).MarksObtained);
    }

    [Fact]
    public async Task Student_can_appeal_once_and_only_for_own_finished_exam()
    {
        using var env = new TestEnv();
        var (_, appeals, session, _) = Build(env);

        var item = await appeals.CreateAsync(env.StudentA.Id, new CreateAppealDto { ExamSessionId = session.Id, Message = "The grade looks wrong" });
        Assert.Equal("Pending", item.Status);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            appeals.CreateAsync(env.StudentA.Id, new CreateAppealDto { ExamSessionId = session.Id, Message = "Second appeal here" }));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            appeals.CreateAsync(env.StudentB.Id, new CreateAppealDto { ExamSessionId = session.Id, Message = "Not my session at all" }));
    }

    [Fact]
    public async Task Appeal_is_not_allowed_before_the_exam_time_ends()
    {
        using var env = new TestEnv();
        var (_, appeals, session, _) = Build(env, examEnded: false);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            appeals.CreateAsync(env.StudentA.Id, new CreateAppealDto { ExamSessionId = session.Id, Message = "Too early to appeal" }));
    }

    [Fact]
    public async Task Only_the_exam_doctor_can_answer_and_only_once()
    {
        using var env = new TestEnv();
        var (_, appeals, session, _) = Build(env);
        var item = await appeals.CreateAsync(env.StudentA.Id, new CreateAppealDto { ExamSessionId = session.Id, Message = "Please review my answer" });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            appeals.RespondAsync(env.DeanA.Id, item.AppealId!.Value, new AppealResponseDto { Decision = "Accepted", Response = "dean tries" }));

        var row = await appeals.RespondAsync(env.DoctorA.Id, item.AppealId!.Value, new AppealResponseDto { Decision = "Accepted", Response = "You are right" });
        Assert.Equal("Accepted", row.Status);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            appeals.RespondAsync(env.DoctorA.Id, item.AppealId!.Value, new AppealResponseDto { Decision = "Rejected", Response = "changed my mind" }));
    }
}
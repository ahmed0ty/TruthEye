using Microsoft.EntityFrameworkCore;
using TruthEye.DTOs;
using TruthEye.Services;

namespace TruthEye.Tests;

public class CourseServiceTests
{
    private static CreateCourseDto NewCourse(int deptId, string name = "Algorithms") =>
        new() { Name = name, Description = "desc", DepartmentId = deptId };

    [Fact]
    public async Task Dean_creates_a_course_only_in_own_faculty()
    {
        using var env = new TestEnv();

        var ok = await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id), env.DeanA.Id);
        Assert.Equal("Algorithms", ok.Name);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Courses.CreateCourseAsync(NewCourse(env.DeptB.Id), env.DeanA.Id));
    }

    [Fact]
    public async Task Dean_of_another_faculty_cannot_read_a_course_even_after_it_was_cached()
    {
        using var env = new TestEnv();
        var course = await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id), env.DeanA.Id);

        // الأولى بتملّا الكاش، والتانية لازم تتمنع
        Assert.Equal(course.Id, (await env.Courses.GetCourseByIdAsync(course.Id, env.DeanA.Id)).Id);
        await Assert.ThrowsAnyAsync<Exception>(() => env.Courses.GetCourseByIdAsync(course.Id, env.DeanB.Id));
    }

    [Fact]
    public async Task Faculty_course_lists_do_not_leak_between_faculties()
    {
        using var env = new TestEnv();
        await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id, "A-course"), env.DeanA.Id);
        await env.Courses.CreateCourseAsync(NewCourse(env.DeptB.Id, "B-course"), env.DeanB.Id);

        var a = await env.Courses.GetFacultyCoursesAsync(env.DeanA.Id);
        var b = await env.Courses.GetFacultyCoursesAsync(env.DeanB.Id);

        Assert.Single(a);
        Assert.Equal("A-course", a[0].Name);
        Assert.Single(b);
        Assert.Equal("B-course", b[0].Name);
    }

    [Fact]
    public async Task Technical_staff_cannot_delete_a_course_but_dean_can_and_it_can_be_reverted()
    {
        using var env = new TestEnv();
        var course = await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id), env.DeanA.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Courses.DeleteCourseAsync(course.Id, env.TechA.Id));
        Assert.False((await env.Db.Courses.SingleAsync(c => c.Id == course.Id)).IsDeleted);

        await env.Courses.DeleteCourseAsync(course.Id, env.DeanA.Id);
        Assert.True((await env.Db.Courses.SingleAsync(c => c.Id == course.Id)).IsDeleted);

        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Delete && l.EntityType == ActivityEntities.Course);
        await env.Courses.RevertCourseActionAsync(log.Id, env.SecretaryA.Id);

        Assert.False((await env.Db.Courses.SingleAsync(c => c.Id == course.Id)).IsDeleted);
    }

    [Fact]
    public async Task Assigning_and_reverting_a_student_works_and_other_faculty_is_blocked()
    {
        using var env = new TestEnv();
        var course = await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id), env.DeanA.Id);

        await env.Courses.AssignStudentToCourseAsync(course.Id, env.StudentA.Id, env.DeanA.Id);
        Assert.Single(await env.Courses.GetCourseStudentsAsync(course.Id, env.DeanA.Id));

        // طالب من كلية تانية
        await Assert.ThrowsAnyAsync<Exception>(() =>
            env.Courses.AssignStudentToCourseAsync(course.Id, env.StudentB.Id, env.DeanA.Id));

        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Assign);
        await env.Courses.RevertCourseActionAsync(log.Id, env.DeanA.Id);

        Assert.Empty(await env.Courses.GetCourseStudentsAsync(course.Id, env.DeanA.Id));
    }

    [Fact]
    public async Task Dean_of_another_faculty_cannot_revert_course_logs()
    {
        using var env = new TestEnv();
        var course = await env.Courses.CreateCourseAsync(NewCourse(env.DeptA.Id), env.DeanA.Id);
        await env.Courses.DeleteCourseAsync(course.Id, env.DeanA.Id);

        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Delete && l.EntityType == ActivityEntities.Course);
        await Assert.ThrowsAnyAsync<Exception>(() => env.Courses.RevertCourseActionAsync(log.Id, env.DeanB.Id));
        Assert.True((await env.Db.Courses.SingleAsync(c => c.Id == course.Id)).IsDeleted);
    }
}
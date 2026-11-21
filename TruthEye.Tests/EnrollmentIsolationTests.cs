using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TruthEye.DTOs;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
namespace TruthEye.Tests;

public class EnrollmentIsolationTests
{
    private static CourseEnrollmentService Svc(TestEnv env) => new(
        new UnitOfWork(env.Db),
                new CacheService(
            new MemoryCache(new MemoryCacheOptions()),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<CacheService>.Instance,
            new ConfigurationBuilder().Build()),
        new SystemLogService(env.Db, new HttpContextAccessor(), NullLogger<SystemLogService>.Instance),
        NullLogger<CourseEnrollmentService>.Instance);

    private static async Task<int> NewCourse(TestEnv env) =>
        (await env.Courses.CreateCourseAsync(
            new CreateCourseDto { Name = "Algo", Description = "d", DepartmentId = env.DeptA.Id }, env.DeanA.Id)).Id;

    [Fact]
    public async Task Dean_enrolls_own_students_and_other_faculty_ids_are_reported_missing()
    {
        using var env = new TestEnv();
        var courseId = await NewCourse(env);

        var r = await Svc(env).EnrollAsync(courseId,
            new BulkEnrollRequest { StudentIds = new() { env.StudentA.Id, env.StudentB.Id } }, env.DeanA.Id);

        Assert.Equal(1, r.Added);
        Assert.Contains($"#{env.StudentB.Id}", r.NotFound);
        Assert.Contains(await env.Db.SystemLogs.ToListAsync(),
            l => l.Action == ActivityActions.BulkAssign && l.FacultyId == env.FacA.Id);
    }

    [Fact]
    public async Task Dean_of_another_faculty_cannot_touch_the_course()
    {
        using var env = new TestEnv();
        var courseId = await NewCourse(env);

        await Assert.ThrowsAnyAsync<Exception>(() => Svc(env).EnrollAsync(courseId,
            new BulkEnrollRequest { StudentIds = new() { env.StudentB.Id } }, env.DeanB.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => Svc(env).UnenrollAsync(courseId, new() { env.StudentA.Id }, env.DeanB.Id));
    }

    [Fact]
    public async Task Department_selection_never_crosses_faculties()
    {
        using var env = new TestEnv();
        var courseId = await NewCourse(env);

        await Assert.ThrowsAnyAsync<Exception>(() => Svc(env).EnrollAsync(courseId,
            new BulkEnrollRequest { DepartmentId = env.DeptB.Id }, env.DeanA.Id));
    }
}
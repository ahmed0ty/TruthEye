using Microsoft.EntityFrameworkCore;
using TruthEye.DTOs;
using TruthEye.Security;
using TruthEye.Services;

namespace TruthEye.Tests;

public class FacultyIsolationTests
{
    private static RegisterStaffDto NewStaff(string role, string email, string nid)
    {
        var dto = new RegisterStaffDto
        {
            Role = role,
            FirstName = "New",
            LastName = "Staff",
            Email = email,
            NationalId = nid,
            PhoneNumber = "+201000000000",
            Password = "Passw0rd!123"
        };
        // لو RegisterUserDto فيه تأكيد باسورد
        dto.GetType().GetProperty("ConfirmPassword")?.SetValue(dto, "Passw0rd!123");
        return dto;
    }

    // ---------- منع التصعيد ----------

    [Fact]
    public async Task Dean_appoints_secretary_inside_own_faculty()
    {
        using var env = new TestEnv();

        var result = await env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Secretary, "new.sec@test.com", "29900000000001"), env.DeanA.Id);

        Assert.Equal(AppRoles.Secretary, result.Role);
        Assert.Equal(env.FacA.Id, result.FacultyId);
        Assert.IsType<TruthEye.Entities.Data.FacultySecretary>(
            await env.Db.Users.SingleAsync(u => u.Id == result.Id));
    }

    [Fact]
    public async Task Secretary_cannot_appoint_another_secretary()
    {
        using var env = new TestEnv();
        var before = await env.Db.Users.CountAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Secretary, "x1@test.com", "29900000000002"), env.SecretaryA.Id));

        Assert.Equal(before, await env.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Secretary_can_appoint_technical_staff()
    {
        using var env = new TestEnv();

        var result = await env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Technical, "x2@test.com", "29900000000003"), env.SecretaryA.Id);

        Assert.Equal(AppRoles.Technical, result.Role);
        Assert.Equal(env.FacA.Id, result.FacultyId);
    }

    [Fact]
    public async Task Technical_staff_cannot_appoint_anyone()
    {
        using var env = new TestEnv();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Technical, "x3@test.com", "29900000000004"), env.TechA.Id));
    }

    [Fact]
    public async Task Staff_cannot_appoint_a_dean_through_the_staff_endpoint()
    {
        using var env = new TestEnv();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Dean, "x4@test.com", "29900000000005"), env.DeanA.Id));
    }

    [Fact]
    public async Task Secretary_cannot_disable_the_dean()
    {
        using var env = new TestEnv();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DisableStaffAsync(env.DeanA.Id, env.SecretaryA.Id));
        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == env.DeanA.Id)).IsActive);
    }

    // ---------- عزل الكليات ----------

    [Fact]
    public async Task Dean_cannot_update_disable_or_delete_a_student_of_another_faculty()
    {
        using var env = new TestEnv();
        var id = env.StudentB.Id;

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DisableStudentAsync(id, env.DeanA.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DeleteStudentAsync(id, env.DeanA.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.UpdateStudentAsync(id,
            new UpdateStudentDto { FirstName = "Hacked", LastName = "Hacked", PhoneNumber = "+201000000000", DepartmentId = env.DeptA.Id, IsActive = true },
            env.DeanA.Id));

        var s = await env.Db.Users.SingleAsync(u => u.Id == id);
        Assert.True(s.IsActive);
        Assert.False(s.IsDeleted);
        Assert.Equal("stuB", s.FirstName);
    }

    [Fact]
    public async Task Dean_cannot_manage_staff_of_another_faculty()
    {
        using var env = new TestEnv();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DisableStaffAsync(env.SecretaryB.Id, env.DeanA.Id));
        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == env.SecretaryB.Id)).IsActive);
    }

    [Fact]
    public async Task Staff_list_and_proctor_list_contain_only_own_faculty()
    {
        using var env = new TestEnv();

        var staff = await env.Auth.GetStaffAsync(env.DeanA.Id);
        Assert.All(staff, s => Assert.Equal(env.FacA.Id, s.FacultyId));
        Assert.DoesNotContain(staff, s => s.Id == env.SecretaryB.Id);

        var proctors = await env.Auth.GetFacultyProctorsAsync(env.DeanA.Id, includeDisabled: true);
        Assert.Single(proctors);
        Assert.Equal(env.ProctorA.Id, proctors[0].Id);
    }

    [Fact]
    public async Task Dean_cannot_disable_a_proctor_of_another_faculty()
    {
        using var env = new TestEnv();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DisableProctorAsync(env.ProctorB.Id, env.DeanA.Id));
        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == env.ProctorB.Id)).IsActive);
    }

    // ---------- تعطيل / حذف / تراجع ----------

    [Fact]
    public async Task Technical_staff_can_disable_but_not_delete()
    {
        using var env = new TestEnv();

        Assert.True(await env.Auth.DisableStudentAsync(env.StudentA.Id, env.TechA.Id));
        Assert.False((await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id)).IsActive);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.DeleteStudentAsync(env.StudentA.Id, env.TechA.Id));
        Assert.False((await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id)).IsDeleted);
    }

    [Fact]
    public async Task Delete_is_soft_and_revert_restores_the_record()
    {
        using var env = new TestEnv();

        await env.Auth.DeleteStudentAsync(env.StudentA.Id, env.DeanA.Id);
        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id)).IsDeleted);

        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Delete && l.EntityId == env.StudentA.Id);
        Assert.Equal(env.FacA.Id, log.FacultyId);

        await env.Auth.RevertMemberActionAsync(log.Id, env.SecretaryA.Id);

        var restored = await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id);
        Assert.False(restored.IsDeleted);
        Assert.True((await env.Db.SystemLogs.SingleAsync(l => l.Id == log.Id)).IsReverted);
    }

    [Fact]
    public async Task Revert_of_disable_brings_the_student_back_to_active()
    {
        using var env = new TestEnv();

        await env.Auth.DisableStudentAsync(env.StudentA.Id, env.DeanA.Id);
        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Disable);

        await env.Auth.RevertMemberActionAsync(log.Id, env.DeanA.Id);

        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id)).IsActive);
    }

    [Fact]
    public async Task Same_action_cannot_be_reverted_twice()
    {
        using var env = new TestEnv();

        await env.Auth.DisableStudentAsync(env.StudentA.Id, env.DeanA.Id);
        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Disable);

        await env.Auth.RevertMemberActionAsync(log.Id, env.DeanA.Id);
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RevertMemberActionAsync(log.Id, env.DeanA.Id));
    }

    [Fact]
    public async Task Dean_of_another_faculty_cannot_revert_this_faculty_log()
    {
        using var env = new TestEnv();

        await env.Auth.DisableStudentAsync(env.StudentA.Id, env.DeanA.Id);
        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Disable);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RevertMemberActionAsync(log.Id, env.DeanB.Id));
        Assert.False((await env.Db.Users.SingleAsync(u => u.Id == env.StudentA.Id)).IsActive);
    }

    [Fact]
    public async Task Technical_staff_cannot_revert()
    {
        using var env = new TestEnv();

        await env.Auth.DisableStudentAsync(env.StudentA.Id, env.DeanA.Id);
        var log = await env.Db.SystemLogs.SingleAsync(l => l.Action == ActivityActions.Disable);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RevertMemberActionAsync(log.Id, env.TechA.Id));
    }

    // ---------- الإيميل المحجوز ----------

    [Fact]
    public async Task Email_and_national_id_of_a_deleted_user_stay_reserved()
    {
        using var env = new TestEnv();
        var deleted = env.ExtraStudent("ghost", deleted: true);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Technical, deleted.Email, "29900000000099"), env.SecretaryA.Id));

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RegisterStaffAsync(
            NewStaff(AppRoles.Technical, "fresh@test.com", deleted.NationalId), env.SecretaryA.Id));
    }
}
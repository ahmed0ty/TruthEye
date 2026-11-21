using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TruthEye.DTOs;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;

namespace TruthEye.Tests;

public class PasswordAndLoginTests
{
    private const string Pw = "Passw0rd!123";

    private static RegisterStaffDto Staff(string email, string nid)
    {
        var dto = new RegisterStaffDto
        {
            Role = AppRoles.Technical, FirstName = "New", LastName = "Staff",
            Email = email, NationalId = nid, PhoneNumber = "+201000000000", Password = Pw
        };
        dto.GetType().GetProperty("ConfirmPassword")?.SetValue(dto, Pw);
        return dto;
    }

    [Fact]
    public async Task Accounts_created_by_staff_must_change_password_and_the_flag_clears()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStaffAsync(Staff("mcp@test.com", "29900000000010"), env.SecretaryA.Id);

        Assert.True((await env.Db.Users.SingleAsync(u => u.Id == created.Id)).MustChangePassword);

        await env.Auth.ChangePasswordAsync(created.Id, new ChangePasswordDto
        {
            CurrentPassword = Pw, NewPassword = "NewPassw0rd!456", ConfirmNewPassword = "NewPassw0rd!456"
        });

        Assert.False((await env.Db.Users.SingleAsync(u => u.Id == created.Id)).MustChangePassword);
    }

    [Fact]
    public async Task Weak_or_unchanged_passwords_are_rejected()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStaffAsync(Staff("weak@test.com", "29900000000011"), env.SecretaryA.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ChangePasswordAsync(created.Id,
            new ChangePasswordDto { CurrentPassword = Pw, NewPassword = "abc12", ConfirmNewPassword = "abc12" }));
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ChangePasswordAsync(created.Id,
            new ChangePasswordDto { CurrentPassword = Pw, NewPassword = "onlyletters", ConfirmNewPassword = "onlyletters" }));
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ChangePasswordAsync(created.Id,
            new ChangePasswordDto { CurrentPassword = Pw, NewPassword = Pw, ConfirmNewPassword = Pw }));
    }

    [Fact]
    public async Task Failed_logins_lock_per_account_and_ip_only()
    {
        using var env = new TestEnv();
        await env.Auth.RegisterStaffAsync(Staff("lock@test.com", "29900000000012"), env.SecretaryA.Id);
        var bad = new LoginDto { Email = "lock@test.com", Password = "wrong-password-1" };

        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.LoginAsync(bad, "1.1.1.1"));

        var locked = await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.LoginAsync(bad, "1.1.1.1"));
        Assert.Contains("Too many", locked.Message);

        // IP تاني لسه بيتعامل عادي (مش مقفول)
        var other = await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.LoginAsync(bad, "2.2.2.2"));
        Assert.Contains("Invalid credentials", other.Message);
    }

    [Fact]
    public async Task Login_attempts_are_logged_and_hidden_from_the_default_view()
    {
        using var env = new TestEnv();
        await env.Auth.RegisterStaffAsync(Staff("log@test.com", "29900000000013"), env.SecretaryA.Id);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            env.Auth.LoginAsync(new LoginDto { Email = "log@test.com", Password = "wrong-pass-1" }, "9.9.9.9"));
        await env.Auth.LoginAsync(new LoginDto { Email = "log@test.com", Password = Pw }, "9.9.9.9");

        var logs = await env.Db.SystemLogs.Where(l => l.EntityType == ActivityEntities.Login).ToListAsync();
        Assert.Contains(logs, l => l.Action == ActivityActions.LoginFailed && l.FacultyId == env.FacA.Id);
        Assert.Contains(logs, l => l.Action == ActivityActions.LoginSuccess);

        var audit = new SystemLogService(env.Db, new HttpContextAccessor(), NullLogger<SystemLogService>.Instance);
        var page = await audit.QueryAsync(new ActivityLogFilter { FacultyId = env.FacA.Id });
        Assert.DoesNotContain(page.Items, i => i.EntityType == ActivityEntities.Login);

        var logins = await audit.QueryAsync(new ActivityLogFilter { FacultyId = env.FacA.Id, EntityType = ActivityEntities.Login });
        Assert.NotEmpty(logins.Items);
    }
    [Fact]
    public async Task Reset_sends_a_one_time_link_and_never_reveals_the_password()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStaffAsync(Staff("reset@test.com", "29900000000014"), env.SecretaryA.Id);
        await env.Auth.ChangePasswordAsync(created.Id, new ChangePasswordDto
        { CurrentPassword = Pw, NewPassword = "Another1Pass", ConfirmNewPassword = "Another1Pass" });

        var masked = await env.Auth.ResetPasswordAsync(created.Id, env.SecretaryA.Id);
        Assert.DoesNotContain("reset@test.com", masked);

        var mail = Assert.Single(env.Mail.Sent);
        Assert.Equal("reset@test.com", mail.To);
        var token = Regex.Match(mail.Body, "token=([A-F0-9]{64})").Groups[1].Value;
        Assert.Equal(64, token.Length);

        await env.Auth.ConfirmPasswordResetAsync(token, "Brand1NewPass");
        var tokens = await env.Auth.LoginAsync(new LoginDto { Email = "reset@test.com", Password = "Brand1NewPass" }, "5.5.5.5");
        Assert.False(tokens.MustChangePassword);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            env.Auth.LoginAsync(new LoginDto { Email = "reset@test.com", Password = "Another1Pass" }, "5.5.5.5"));

        // الرابط ما يتستخدمش مرتين
        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ConfirmPasswordResetAsync(token, "Third1Pass"));

        Assert.Contains(await env.Db.SystemLogs.ToListAsync(),
            l => l.Action == ActivityActions.ResetPassword && l.EntityId == created.Id && l.FacultyId == env.FacA.Id);
    }

    [Fact]
    public async Task Expired_reset_link_is_rejected()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStaffAsync(Staff("exp@test.com", "29900000000015"), env.SecretaryA.Id);
        await env.Auth.ResetPasswordAsync(created.Id, env.SecretaryA.Id);
        var token = Regex.Match(env.Mail.Sent.Single().Body, "token=([A-F0-9]{64})").Groups[1].Value;

        foreach (var t in env.Db.PasswordResetTokens) t.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        env.Db.SaveChanges();

        await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ConfirmPasswordResetAsync(token, "Brand1NewPass"));
    }

[Fact]
public async Task Reset_password_respects_hierarchy_and_faculty()
{
    using var env = new TestEnv();

    // الأمين مايعملش Reset للعميد
    await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ResetPasswordAsync(env.DeanA.Id, env.SecretaryA.Id));
    // التيم الفني مايعملش Reset لدكتور
    await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ResetPasswordAsync(env.DoctorA.Id, env.TechA.Id));
    // عميد "أ" مايعملش Reset لطالب كلية "ب"
    await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ResetPasswordAsync(env.StudentB.Id, env.DeanA.Id));
    // محدش يعمل Reset لنفسه
    await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.ResetPasswordAsync(env.DeanA.Id, env.DeanA.Id));

    // التيم الفني يعمل Reset لطالب كليته، والعميد لدكتور كليته
    Assert.NotEmpty(await env.Auth.ResetPasswordAsync(env.StudentA.Id, env.TechA.Id));
    Assert.NotEmpty(await env.Auth.ResetPasswordAsync(env.DoctorA.Id, env.DeanA.Id));
}


[Fact]
public async Task Another_device_is_blocked_while_the_session_is_alive_and_allowed_after_it_goes_quiet()
{
    using var env = new TestEnv();
    var c = await env.Auth.RegisterStaffAsync(Staff("dev@test.com", "29900000000040"), env.SecretaryA.Id);

    await env.Auth.LoginAsync(new LoginDto { Email = "dev@test.com", Password = Pw }, "1.1.1.1", "devA");

    // نفس الجهاز: مسموح
    await env.Auth.LoginAsync(new LoginDto { Email = "dev@test.com", Password = Pw }, "1.1.1.1", "devA");

    // جهاز تاني والجلسة حية: ممنوع
    await Assert.ThrowsAnyAsync<Exception>(() =>
        env.Auth.LoginAsync(new LoginDto { Email = "dev@test.com", Password = Pw }, "2.2.2.2", "devB"));

    // النبض وقف: الجهاز التاني يدخل
    foreach (var s in env.Db.UserSessions.Where(x => x.UserId == c.Id)) s.LastSeenAt = DateTime.UtcNow.AddMinutes(-5);
    env.Db.SaveChanges();
    await env.Auth.LoginAsync(new LoginDto { Email = "dev@test.com", Password = Pw }, "2.2.2.2", "devB");
}

[Fact]
public async Task Refresh_fails_after_the_session_was_replaced()
{
    using var env = new TestEnv();
    var c = await env.Auth.RegisterStaffAsync(Staff("rep@test.com", "29900000000041"), env.SecretaryA.Id);

    var t1 = await env.Auth.LoginAsync(new LoginDto { Email = "rep@test.com", Password = Pw }, "1.1.1.1", "devA");
    foreach (var s in env.Db.UserSessions.Where(x => x.UserId == c.Id)) s.LastSeenAt = DateTime.UtcNow.AddMinutes(-5);
    env.Db.SaveChanges();
    await env.Auth.LoginAsync(new LoginDto { Email = "rep@test.com", Password = Pw }, "2.2.2.2", "devB");

    await Assert.ThrowsAnyAsync<Exception>(() => env.Auth.RefreshAsync(t1.RefreshToken));
}
}
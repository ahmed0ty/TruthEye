using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TruthEye.Data.Enum;
using TruthEye.DTOs;
using TruthEye.Entities.Data;
using TruthEye.Exceptions;

namespace TruthEye.Tests;

/// تدفق "رابط الإعداد": الأدمن ينشئ الحساب بدون ما يشوف باسورد، والطالب يختار الباسورد بنفسه من الإيميل.
public class SetupLinkTests
{
    private static readonly Regex TokenInLink = new(@"token=([0-9A-F]{64})");

    private static RegisterStudentDto Student(string email, string nid, bool sendLink, int deptId)
    {
        var dto = new RegisterStudentDto
        {
            FirstName = "New", LastName = "Student", Email = email, NationalId = nid,
            PhoneNumber = "+201000000001", DepartmentId = deptId, Year = Year.First,
            SendSetupLink = sendLink
        };
        return dto;
    }

    [Fact]
    public async Task Created_student_gets_one_setup_email_with_a_link_and_no_password()
    {
        using var env = new TestEnv();
        var dto = Student("setup1@test.com", "29900000000020", sendLink: true, env.DeptA.Id);

        var created = await env.Auth.RegisterStudentAsync(dto, env.DeanA.Id);

        var mail = Assert.Single(env.Mail.Sent);
        Assert.Equal("setup1@test.com", mail.To);
        Assert.Equal("Set your TruthEye password", mail.Subject);
        Assert.Matches(TokenInLink, mail.Body);
        Assert.DoesNotContain("Aa1", mail.Body); // الباسورد العشوائي مابيتبعتش أبدًا

        var user = await env.Db.Users.SingleAsync(u => u.Id == created.Id);
        Assert.True(user.MustChangePassword);

        var token = await env.Db.PasswordResetTokens.SingleAsync(t => t.UserId == created.Id);
        Assert.Equal(env.DeanA.Id, token.RequestedBy);
        Assert.Null(token.UsedAt);
    }

    [Fact]
    public async Task Setup_link_lets_the_student_choose_a_password_once()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStudentAsync(
            Student("setup2@test.com", "29900000000021", sendLink: true, env.DeptA.Id), env.DeanA.Id);
        var token = TokenInLink.Match(env.Mail.Sent.Single().Body).Groups[1].Value;

        await env.Auth.ConfirmPasswordResetAsync(token, "MyOwnPassw0rd!");

        var user = await env.Db.Users.SingleAsync(u => u.Id == created.Id);
        Assert.False(user.MustChangePassword);

        // الرابط بيتستخدم مرة واحدة بس
        await Assert.ThrowsAsync<BadRequestException>(() =>
            env.Auth.ConfirmPasswordResetAsync(token, "AnotherPassw0rd!"));
    }

    [Fact]
    public async Task Without_setup_link_a_password_is_required()
    {
        using var env = new TestEnv();
        var dto = Student("setup3@test.com", "29900000000022", sendLink: false, env.DeptA.Id);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            env.Auth.RegisterStudentAsync(dto, env.DeanA.Id));
        Assert.Empty(env.Mail.Sent);
    }

    [Fact]
    public async Task Failed_email_still_creates_the_account_but_kills_the_link()
    {
        using var env = new TestEnv(new FailingEmailSender());
        var created = await env.Auth.RegisterStudentAsync(
            Student("setup4@test.com", "29900000000023", sendLink: true, env.DeptA.Id), env.DeanA.Id);

        // الحساب اتعمل، والتوكن اتعلّم مستخدم عشان ماييجيش رابط شغال من غير ما حد يعرفه
        Assert.NotNull(await env.Db.Users.FindAsync(created.Id));
        var token = await env.Db.PasswordResetTokens.SingleAsync(t => t.UserId == created.Id);
        Assert.NotNull(token.UsedAt);
    }

    [Fact]
    public async Task Admin_reset_sends_a_new_link_and_voids_the_old_one()
    {
        using var env = new TestEnv();
        var created = await env.Auth.RegisterStudentAsync(
            Student("setup5@test.com", "29900000000024", sendLink: true, env.DeptA.Id), env.DeanA.Id);
        var oldToken = TokenInLink.Match(env.Mail.Sent.Single().Body).Groups[1].Value;

        await env.Auth.ResetPasswordAsync(created.Id, env.DeanA.Id);

        var resetMail = env.Mail.Sent.Last();
        Assert.Equal("Reset your TruthEye password", resetMail.Subject);
        var newToken = TokenInLink.Match(resetMail.Body).Groups[1].Value;
        Assert.NotEqual(oldToken, newToken);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            env.Auth.ConfirmPasswordResetAsync(oldToken, "FreshPassw0rd!"));
        await env.Auth.ConfirmPasswordResetAsync(newToken, "FreshPassw0rd!");
    }
}

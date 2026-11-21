using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;

namespace TruthEye.Tests;

public class StaffRulesTests
{
    private static readonly string[] AllRoles =
    {
        AppRoles.SuperAdmin, AppRoles.Dean, AppRoles.Secretary, AppRoles.Technical,
        AppRoles.Doctor, AppRoles.Student, AppRoles.Proctor, AppRoles.Support
    };

    [Theory]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Dean, true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Secretary, false)]
    [InlineData(AppRoles.Dean, AppRoles.Secretary, true)]
    [InlineData(AppRoles.Dean, AppRoles.Technical, true)]
    [InlineData(AppRoles.Dean, AppRoles.Dean, false)]
    [InlineData(AppRoles.Dean, AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.Secretary, AppRoles.Technical, true)]
    [InlineData(AppRoles.Secretary, AppRoles.Secretary, false)]
    [InlineData(AppRoles.Secretary, AppRoles.Dean, false)]
    [InlineData(AppRoles.Technical, AppRoles.Technical, false)]
    [InlineData(AppRoles.Technical, AppRoles.Secretary, false)]
    [InlineData(AppRoles.Technical, AppRoles.Dean, false)]
    public void CanManageStaff_follows_the_hierarchy(string actor, string target, bool expected) =>
        Assert.Equal(expected, StaffRules.CanManageStaff(actor, target));

    [Fact]
    public void Nobody_can_manage_their_own_level_or_above()
    {
        var rank = new Dictionary<string, int>
        {
            [AppRoles.SuperAdmin] = 4, [AppRoles.Dean] = 3, [AppRoles.Secretary] = 2, [AppRoles.Technical] = 1
        };

        foreach (var actor in AllRoles)
            foreach (var target in AllRoles)
            {
                if (!StaffRules.CanManageStaff(actor, target)) continue;
                Assert.True(rank.ContainsKey(actor) && rank.ContainsKey(target), $"{actor} -> {target}");
                Assert.True(rank[actor] > rank[target], $"{actor} must outrank {target}");
            }
    }

    [Fact]
    public void Doctors_students_and_proctors_manage_nobody()
    {
        foreach (var actor in new[] { AppRoles.Doctor, AppRoles.Student, AppRoles.Proctor, AppRoles.Support })
            foreach (var target in AllRoles)
                Assert.False(StaffRules.CanManageStaff(actor, target));
    }

    [Theory]
    [InlineData(AppRoles.Dean, true)]
    [InlineData(AppRoles.Secretary, true)]
    [InlineData(AppRoles.Technical, false)]
    [InlineData(AppRoles.Student, false)]
    public void Only_dean_and_secretary_can_delete(string role, bool expected) =>
        Assert.Equal(expected, StaffRules.CanDeleteMembers(role));

    [Fact]
    public void Technical_staff_cannot_see_the_activity_log() =>
        Assert.False(StaffRules.CanViewActivityLog(AppRoles.Technical));

    [Fact]
    public void RoleOf_checks_subclasses_before_Admin()
    {
        Assert.Equal(AppRoles.Dean, StaffRules.RoleOf(new Admin()));
        Assert.Equal(AppRoles.Secretary, StaffRules.RoleOf(new FacultySecretary()));
        Assert.Equal(AppRoles.Technical, StaffRules.RoleOf(new TechnicalStaff()));
        Assert.Equal(AppRoles.SuperAdmin, StaffRules.RoleOf(new SuperAdmin()));
        Assert.Equal(AppRoles.Student, StaffRules.RoleOf(new Student()));
    }
}
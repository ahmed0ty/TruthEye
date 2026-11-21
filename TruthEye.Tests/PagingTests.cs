using TruthEye.DTOs;

namespace TruthEye.Tests;

public class PagingTests
{
    [Fact]
    public async Task Students_page_contains_only_own_faculty_and_pagesize_is_capped()
    {
        using var env = new TestEnv();

        var r = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { PageSize = 5000 });

        Assert.Equal(100, r.PageSize);
        Assert.Contains(r.Items, s => s.Id == env.StudentA.Id);
        Assert.DoesNotContain(r.Items, s => s.Id == env.StudentB.Id);
    }

    [Fact]
    public async Task Paging_splits_results_without_overlap_and_reports_the_total()
    {
        using var env = new TestEnv();
        for (var i = 0; i < 4; i++) env.ExtraStudent($"extra{i}");   // + StudentA = 5

        var p1 = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { Page = 1, PageSize = 2 });
        var p3 = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { Page = 3, PageSize = 2 });

        Assert.Equal(5, p1.Total);
        Assert.Equal(2, p1.Items.Count);
        Assert.Single(p3.Items);
        Assert.Empty(p1.Items.Select(s => s.Id).Intersect(p3.Items.Select(s => s.Id)));
    }

    [Fact]
    public async Task Search_status_and_sort_work_on_the_server()
    {
        using var env = new TestEnv();
        env.ExtraStudent("alpha");
        var zed = env.ExtraStudent("zed");
        await env.Auth.DisableStudentAsync(zed.Id, env.DeanA.Id);

        var found = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { Search = "alph" });
        Assert.Single(found.Items);
        Assert.Equal("alpha", found.Items[0].FirstName);

        var inactive = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { Status = "inactive" });
        Assert.Single(inactive.Items);
        Assert.Equal(zed.Id, inactive.Items[0].Id);

        var desc = await env.Auth.GetFacultyStudentsPagedAsync(env.DeanA.Id, new PageQuery { Sort = "name", Dir = "desc" });
        Assert.Equal("zed", desc.Items[0].FirstName);
    }

    [Fact]
    public async Task Proctor_page_is_limited_to_own_faculty()
    {
        using var env = new TestEnv();

        var r = await env.Auth.GetFacultyProctorsPagedAsync(env.DeanA.Id, new PageQuery());

        Assert.Single(r.Items);
        Assert.Equal(env.ProctorA.Id, r.Items[0].Id);
    }
}
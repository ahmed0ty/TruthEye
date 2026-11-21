using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TruthEye.Controllers;
using TruthEye.Entities;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Tests;

/// جهات الاتصال: الطالب يشوف الدكتور اللي بيدرّسله، والدكتور يشوف الطلبة اللي في كورساته بس.
public class ContactsTests
{
    private static async Task<HashSet<int>> ContactIdsAsync(TestEnv env, int userId)
    {
        var controller = new ContactsController(new UnitOfWork(env.Db),
            new ChatLockService(env.Db, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"))
                }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.Get());
        var data = (System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        return data.Cast<object>()
            .Select(c => (int)c.GetType().GetProperty("Id")!.GetValue(c)!)
            .ToHashSet();
    }

    [Fact]
    public async Task Student_sees_only_the_doctor_who_teaches_a_shared_course()
    {
        using var env = new TestEnv();
        var otherDoctor = new Doctor
        {
            FirstName = "docB", LastName = "Test", Email = "docb@test.com", PasswordHash = "x",
            NationalId = "30000000000090", PhoneNumber = "01000000000", ProfileImage = "d.jpg",
            IsActive = true, RoleId = TestEnv.RoleDoctor, CreatedAt = DateTime.UtcNow,
            UniversityId = env.StudentA.UniversityId, FacultyId = env.FacA.Id,
            DepartmentId = env.DeptA.Id, Specialization = "S"
        };
        env.Db.Add(otherDoctor);
        env.Db.Add(new Course
        {
            Name = "Course A", Description = "d", DepartmentId = env.DeptA.Id,
            RegisteredStudents = { env.StudentA },
            Instructors = { env.DoctorA }
        });
        env.Db.SaveChanges();

        var ids = await ContactIdsAsync(env, env.StudentA.Id);

        Assert.Contains(env.DoctorA.Id, ids);
        Assert.DoesNotContain(otherDoctor.Id, ids);
    }

    [Fact]
    public async Task Doctor_sees_only_students_registered_in_their_courses()
    {
        using var env = new TestEnv();
        var outsider = env.ExtraStudent("outsider");
        env.Db.Add(new Course
        {
            Name = "Course A", Description = "d", DepartmentId = env.DeptA.Id,
            RegisteredStudents = { env.StudentA },
            Instructors = { env.DoctorA }
        });
        env.Db.SaveChanges();

        var ids = await ContactIdsAsync(env, env.DoctorA.Id);

        Assert.Contains(env.StudentA.Id, ids);
        Assert.DoesNotContain(outsider.Id, ids);
    }
}

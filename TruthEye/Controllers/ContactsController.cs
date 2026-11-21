using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatLockService _lock;

        public ContactsController(IUnitOfWork unitOfWork, IChatLockService chatLock)
        {
            _unitOfWork = unitOfWork;
            _lock = chatLock;
        }

        private sealed record Contact(int Id, string Name, string Role, string? ProfileImage);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var me = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (me == null || !me.IsActive) return Unauthorized();

            if ((await _lock.GetAsync(ChatRules.FacultyOf(me))).Locked)
                return Ok(new { success = true, data = new List<Contact>() });

            var result = new List<Contact>();

            Contact Make(User u) =>
                new(u.Id, (u is Doctor ? "Dr. " : "") + $"{u.FirstName} {u.LastName}", StaffRules.RoleOf(u), u.ProfileImage);

            async Task Add(IEnumerable<User> users)
            {
                foreach (var u in users)
                    if (await ChatRules.CanChat(_unitOfWork, me, u)) result.Add(Make(u));
            }

            switch (me)
            {
                case Student s:
                    {
                        // الدكاترة في كلية الطالب بس، ومنهم اللي بيدرّسوا كورس مشترك معاه (استعلام واحد بدل واحد لكل دكتور)
                        var sharedDoctors = await SharedInstructorIdsAsync(s.Id);
                        var doctors = await _unitOfWork.Repository<Doctor>().FindAsync(doc => doc.IsActive && doc.FacultyId == s.FacultyId);
                        foreach (var doc in doctors)
                            if (sharedDoctors.Contains(doc.Id)) result.Add(Make(doc));
                        break;
                    }

                case Doctor d:
                    {
                        // الطلبة اللي مسجلين في كورس بيدرّسه، وفي كلية الدكتور (استعلام واحد بدل واحد لكل طالب)
                        var sharedStudents = await SharedStudentIdsAsync(d.Id);
                        var students = await _unitOfWork.Repository<Student>().FindAsync(stu => stu.IsActive && stu.FacultyId == d.FacultyId);
                        foreach (var stu in students)
                            if (sharedStudents.Contains(stu.Id)) result.Add(Make(stu));
                        break;
                    }

                case Admin a:
                    await Add(await _unitOfWork.Repository<Student>().FindAsync(s => s.IsActive && s.FacultyId == a.FacultyId));
                    await Add(await _unitOfWork.Repository<Doctor>().FindAsync(d => d.IsActive && d.FacultyId == a.FacultyId));
                    await Add(await _unitOfWork.Repository<Proctor>().FindAsync(p => p.IsActive && p.FacultyId == a.FacultyId));
                    break;

                case Proctor p when p.FacultyId.HasValue:
                    await Add(await _unitOfWork.Repository<Admin>().FindAsync(a => a.IsActive && a.FacultyId == p.FacultyId));
                    break;

                case Support:
                case SuperAdmin:
                    await Add(await _unitOfWork.Repository<Admin>().FindAsync(a => a.IsActive));
                    break;
            }

            return Ok(new { success = true, data = result.OrderBy(c => c.Name) });
        }

        private async Task<HashSet<int>> SharedInstructorIdsAsync(int studentId)
        {
            var courses = await _unitOfWork.Repository<Course>().FindAsync(
                c => c.RegisteredStudents.Any(z => z.Id == studentId),
                c => c.Instructors);
            return courses.SelectMany(c => c.Instructors).Select(i => i.Id).ToHashSet();
        }

        private async Task<HashSet<int>> SharedStudentIdsAsync(int doctorId)
        {
            var courses = await _unitOfWork.Repository<Course>().FindAsync(
                c => c.Instructors.Any(z => z.Id == doctorId),
                c => c.RegisteredStudents);
            return courses.SelectMany(c => c.RegisteredStudents).Select(x => x.Id).ToHashSet();
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var me = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (me == null || !me.IsActive) return Unauthorized();

            var info = await _lock.GetAsync(ChatRules.FacultyOf(me));
            return Ok(new { success = true, data = new { locked = info.Locked, until = info.Until } });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContactsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        private sealed record Contact(int Id, string Name, string Role, string? ProfileImage);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var result = new List<Contact>();

            string Full(User u) => $"{u.FirstName} {u.LastName}";

            Contact Make(User u, string role, bool doctor = false)
                => new(u.Id, (doctor ? "Dr. " : "") + Full(u), role, u.ProfileImage);

            if (role == "Student")
            {
                var me = await _unitOfWork.Repository<Student>().GetByIdAsync(id, s => s.RegisteredCourses);
                var courseIds = me?.RegisteredCourses.Select(c => c.Id).ToList() ?? new List<int>();
                var courses = await _unitOfWork.Repository<Course>()
                    .FindAsync(c => courseIds.Contains(c.Id), c => c.Instructors);
                foreach (var d in courses.SelectMany(c => c.Instructors).DistinctBy(d => d.Id))
                    result.Add(Make(d, "Doctor", true));
            }
            else if (role == "Doctor")
            {
                var me = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id, d => d.TaughtCourses);
                var courseIds = me?.TaughtCourses.Select(c => c.Id).ToList() ?? new List<int>();
                var courses = await _unitOfWork.Repository<Course>()
                    .FindAsync(c => courseIds.Contains(c.Id), c => c.RegisteredStudents);
                foreach (var s in courses.SelectMany(c => c.RegisteredStudents).DistinctBy(s => s.Id))
                    result.Add(Make(s, "Student"));
            }
            else if (role == "Admin")
            {
                var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(id);
                if (admin != null)
                {
                    foreach (var s in await _unitOfWork.Repository<Student>().FindAsync(x => x.FacultyId == admin.FacultyId && x.IsActive))
                        result.Add(Make(s, "Student"));
                    foreach (var d in await _unitOfWork.Repository<Doctor>().FindAsync(x => x.FacultyId == admin.FacultyId && x.IsActive))
                        result.Add(Make(d, "Doctor", true));
                }
            }
            else
            {
                foreach (var a in await _unitOfWork.Repository<Admin>().FindAsync(x => x.IsActive))
                    result.Add(Make(a, "Admin"));
            }

            result.RemoveAll(c => c.Id == id);
            return Ok(new { success = true, data = result.OrderBy(c => c.Name) });
        }
    }
}
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.Swagger;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DepartmentController> _logger;
        private readonly IMapper _mapper;
        private readonly ISystemLogService _audit;

        private static readonly Expression<Func<Department, object>>[] Includes =
        {
            d => d.Faculty,
            d => d.Faculty.University,
            d => d.Courses
        };

        public DepartmentController(IUnitOfWork unitOfWork, ILogger<DepartmentController> logger,
            IMapper mapper, ISystemLogService audit)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _audit = audit;
        }

        private int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)) return id;
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        private string CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        private bool IsSuper => CurrentRole == AppRoles.SuperAdmin;

        private IActionResult Denied(string msg) =>
            StatusCode(403, new ApiErrorResponse { Success = false, Error = msg });

        private IActionResult Error(string msg) =>
            BadRequest(new ApiErrorResponse { Success = false, Error = msg });

        /// <summary>عميد / أمين / تيم فني نشط. بيرجع null لو الحساب مش مسموح.</summary>
        private async Task<Admin?> CurrentStaff()
        {
            if (!StaffRules.IsFacultyStaff(CurrentRole)) return null;
            var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(GetCurrentUserId());
            return admin is { IsActive: true } ? admin : null;
        }

        private IActionResult OkDepartments(IEnumerable<Department> departments, string message) =>
            Ok(new ApiResponse<IEnumerable<DepartmentDto>>
            {
                Success = true,
                Message = message,
                Data = _mapper.Map<IEnumerable<DepartmentDto>>(departments)
            });

        private AuditEntry Entry(string action, Department d, string description, object? oldV = null, object? newV = null) => new()
        {
            ActorId = GetCurrentUserId(),
            ActorRole = CurrentRole,
            FacultyId = d.FacultyId,
            Action = action,
            EntityType = ActivityEntities.Department,
            EntityId = d.Id,
            Description = description,
            OldValues = oldV,
            NewValues = newV
        };

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                if (IsSuper)
                    return OkDepartments(await _unitOfWork.Repository<Department>().GetAllAsync(Includes),
                        "All departments retrieved successfully");

                var staff = await CurrentStaff();
                if (staff == null) return Denied("You don't have permission to view departments");

                var departments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.FacultyId == staff.FacultyId, Includes);
                return OkDepartments(departments, "Departments for your faculty retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve departments");
                return Error("An error occurred while retrieving departments");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            try
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id, Includes);
                if (department == null)
                    return NotFound(new ApiErrorResponse { Success = false, Error = "Department not found" });

                if (!IsSuper)
                {
                    var staff = await CurrentStaff();
                    if (staff == null || staff.FacultyId != department.FacultyId)
                        return Denied("You don't have permission to access this department");
                }

                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Message = "Department retrieved successfully",
                    Data = _mapper.Map<DepartmentDto>(department)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve department {DepartmentId}", id);
                return Error("An error occurred while retrieving the department");
            }
        }

        [Authorize(Roles = AppRoles.FacultyStaffAndSuper)]
        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Validation failed",
                        ErrorCode = "VALIDATION_ERROR",
                        ValidationErrors = GetModelStateErrors()
                    });

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(dto.FacultyId);
                if (faculty == null) return Error("Faculty not found");

                if (!IsSuper)
                {
                    var staff = await CurrentStaff();
                    if (staff == null || staff.FacultyId != dto.FacultyId)
                        return Denied("You can only create departments in your faculty");
                }

                var exists = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.Name.ToLower() == dto.Name.ToLower() && d.FacultyId == dto.FacultyId);
                if (exists.Any()) return Error("Department with this name already exists in the selected faculty");

                var department = _mapper.Map<Department>(dto);
                await _unitOfWork.Repository<Department>().AddAsync(department);
                await _unitOfWork.SaveChangesAsync();

                await _audit.LogAsync(Entry(ActivityActions.Create, department, $"Created department {department.Name}",
                    null, new { department.Name, department.Description }));

                var created = await _unitOfWork.Repository<Department>().GetByIdAsync(department.Id, Includes);
                return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id },
                    new ApiResponse<DepartmentDto>
                    {
                        Success = true,
                        Message = "Department created successfully",
                        Data = _mapper.Map<DepartmentDto>(created)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create department");
                return Error("An error occurred while creating the department");
            }
        }

        [Authorize(Roles = AppRoles.FacultyStaffAndSuper)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Validation failed",
                        ValidationErrors = GetModelStateErrors()
                    });

                var existing = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new ApiErrorResponse { Success = false, Error = "Department not found" });

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(dto.FacultyId);
                if (faculty == null) return Error("Faculty not found");

                if (!IsSuper)
                {
                    var staff = await CurrentStaff();
                    if (staff == null || staff.FacultyId != dto.FacultyId || staff.FacultyId != existing.FacultyId)
                        return Denied("You can only update departments in your faculty");
                }

                var duplicate = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.Name.ToLower() == dto.Name.ToLower() && d.FacultyId == dto.FacultyId && d.Id != id);
                if (duplicate.Any()) return Error("Another department with this name already exists in the selected faculty");

                var before = new { existing.Name, existing.Description, existing.FacultyId };

                existing.Name = dto.Name;
                existing.Description = dto.Description;
                existing.FacultyId = dto.FacultyId;

                _audit.Stage(Entry(ActivityActions.Update, existing, $"Updated department {existing.Name}",
                    before, new { existing.Name, existing.Description, existing.FacultyId }));

                await _unitOfWork.Repository<Department>().UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                var updated = await _unitOfWork.Repository<Department>().GetByIdAsync(id, Includes);
                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Message = "Department updated successfully",
                    Data = _mapper.Map<DepartmentDto>(updated)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update department {DepartmentId}", id);
                return Error("An error occurred while updating the department");
            }
        }

        /// <summary>حذف القسم حذف فعلي، فللعميد والأمين ورئيس الجامعة بس.</summary>
        [Authorize(Roles = AppRoles.ManagersAndSuper)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id, d => d.Courses);
                if (department == null)
                    return NotFound(new ApiErrorResponse { Success = false, Error = "Department not found" });

                if (!IsSuper)
                {
                    var staff = await CurrentStaff();
                    if (staff == null || staff.FacultyId != department.FacultyId)
                        return Denied("You can only delete departments from your faculty");
                }

                if (department.Courses?.Any() == true)
                    return Error("Cannot delete department that has courses. Please delete the courses first.");

                _audit.Stage(Entry(ActivityActions.Delete, department, $"Deleted department {department.Name}",
                    new { department.Name, department.Description, department.FacultyId }));

                await _unitOfWork.Repository<Department>().DeleteAsync(department);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete department {DepartmentId}", id);
                return Error("An error occurred while deleting the department");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDepartments([FromQuery] string? searchTerm)
        {
            try
            {
                int? facultyId = null;
                if (!IsSuper)
                {
                    var staff = await CurrentStaff();
                    if (staff == null) return Denied("You don't have permission to search departments");
                    facultyId = staff.FacultyId;
                }

                var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm;
                var departments = await _unitOfWork.Repository<Department>().FindAsync(
                    d => (facultyId == null || d.FacultyId == facultyId)
                         && (term == null || d.Name.Contains(term) || d.Description.Contains(term)),
                    Includes);

                return OkDepartments(departments, "Departments search completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search departments");
                return Error("An error occurred while searching departments");
            }
        }

        [HttpGet("my-faculty")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetMyFacultyDepartments()
        {
            try
            {
                var staff = await CurrentStaff();
                if (staff == null) return Denied("Account not found or disabled");

                var departments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.FacultyId == staff.FacultyId, Includes);
                return OkDepartments(departments, "Departments for your faculty retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve my-faculty departments");
                return Error("An error occurred while retrieving departments");
            }
        }

        private Dictionary<string, string[]> GetModelStateErrors() =>
            ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
    }
}
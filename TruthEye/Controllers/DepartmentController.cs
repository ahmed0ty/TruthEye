using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq.Expressions;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
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

        public DepartmentController(IUnitOfWork unitOfWork, ILogger<DepartmentController> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all departments for current admin's faculty",
            Description = "Retrieves all departments for the current admin's faculty. SuperAdmin can see all departments.",
            OperationId = "GetAllDepartments"
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepartmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                Expression<Func<Department, bool>> filter;
                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                if (currentUserRole == "SuperAdmin")
                {
                    var departments = await _unitOfWork.Repository<Department>().GetAllAsync(includes);
                    var departmentDtos = _mapper.Map<IEnumerable<DepartmentDto>>(departments);

                    return Ok(new ApiResponse<IEnumerable<DepartmentDto>>
                    {
                        Success = true,
                        Message = "All departments retrieved successfully",
                        Data = departmentDtos
                    });
                }
                else if (currentUserRole == "Admin")
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);
                    if (admin == null)
                    {
                        return Unauthorized(new ApiErrorResponse
                        {
                            Success = false,
                            Error = "Admin not found"
                        });
                    }

                    var departments = await _unitOfWork.Repository<Department>()
                        .FindAsync(d => d.FacultyId == admin.FacultyId, includes);

                    var departmentDtos = _mapper.Map<IEnumerable<DepartmentDto>>(departments);

                    return Ok(new ApiResponse<IEnumerable<DepartmentDto>>
                    {
                        Success = true,
                        Message = $"Departments for your faculty retrieved successfully",
                        Data = departmentDtos
                    });
                }
                else
                {
                    return Forbid("You don't have permission to view departments");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve departments");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving departments"
                });
            }
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Get department by ID",
            Description = "Retrieves a specific department by ID. Admin can only access departments in their faculty.",
            OperationId = "GetDepartmentById"
        )]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            try
            {
                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id, includes);

                if (department == null)
                {
                    _logger.LogWarning("Department with ID {DepartmentId} not found", id);
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Department not found"
                    });
                }

                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (currentUserRole == "Admin")
                {
                    var currentUserId = GetCurrentUserId();
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);

                    if (admin == null || admin.FacultyId != department.FacultyId)
                    {
                        return Forbid("You don't have permission to access this department");
                    }
                }

                var departmentDto = _mapper.Map<DepartmentDto>(department);
                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Message = "Department retrieved successfully",
                    Data = departmentDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve department with ID {DepartmentId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving the department"
                });
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create new department",
            Description = "Creates a new department for a faculty. Admin can only create departments in their faculty.",
            OperationId = "CreateDepartment"
        )]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Validation failed",
                        ErrorCode = "VALIDATION_ERROR",
                        ValidationErrors = GetModelStateErrors()
                    });
                }

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(createDepartmentDto.FacultyId);
                if (faculty == null)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty not found"
                    });
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole == "Admin")
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);
                    if (admin == null || admin.FacultyId != createDepartmentDto.FacultyId)
                    {
                        return Forbid("You can only create departments in your faculty");
                    }
                }

                var existingDepartments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.Name.ToLower() == createDepartmentDto.Name.ToLower()
                                 && d.FacultyId == createDepartmentDto.FacultyId);

                if (existingDepartments.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Department with this name already exists in the selected faculty"
                    });
                }

                var department = _mapper.Map<Department>(createDepartmentDto);

                await _unitOfWork.Repository<Department>().AddAsync(department);
                await _unitOfWork.SaveChangesAsync();

                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                var createdDepartment = await _unitOfWork.Repository<Department>().GetByIdAsync(department.Id, includes);
                var departmentDto = _mapper.Map<DepartmentDto>(createdDepartment);

                _logger.LogInformation("Department created with ID {DepartmentId}: {DepartmentName} in faculty {FacultyId}",
                    department.Id, department.Name, department.FacultyId);

                return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Message = "Department created successfully",
                    Data = departmentDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create department");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while creating the department"
                });
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update department",
            Description = "Updates an existing department's information. Admin can only update departments in their faculty.",
            OperationId = "UpdateDepartment"
        )]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Validation failed",
                        ValidationErrors = GetModelStateErrors()
                    });
                }

                var existingDepartment = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
                if (existingDepartment == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Department not found"
                    });
                }

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(updateDepartmentDto.FacultyId);
                if (faculty == null)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty not found"
                    });
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole == "Admin")
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);
                    if (admin == null || admin.FacultyId != updateDepartmentDto.FacultyId)
                    {
                        return Forbid("You can only update departments in your faculty");
                    }

                    if (existingDepartment.FacultyId != admin.FacultyId)
                    {
                        return Forbid("You can only update departments in your faculty");
                    }
                }

                var duplicateDepartments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.Name.ToLower() == updateDepartmentDto.Name.ToLower()
                                 && d.FacultyId == updateDepartmentDto.FacultyId
                                 && d.Id != id);

                if (duplicateDepartments.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Another department with this name already exists in the selected faculty"
                    });
                }

                existingDepartment.Name = updateDepartmentDto.Name;
                existingDepartment.Description = updateDepartmentDto.Description;
                existingDepartment.FacultyId = updateDepartmentDto.FacultyId;

                await _unitOfWork.Repository<Department>().UpdateAsync(existingDepartment);
                await _unitOfWork.SaveChangesAsync();

                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                var updatedDepartment = await _unitOfWork.Repository<Department>().GetByIdAsync(id, includes);
                var departmentDto = _mapper.Map<DepartmentDto>(updatedDepartment);

                _logger.LogInformation("Department with ID {DepartmentId} updated", id);

                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Message = "Department updated successfully",
                    Data = departmentDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update department with ID {DepartmentId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while updating the department"
                });
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete department",
            Description = "Soft deletes a department. Admin can only delete departments in their faculty.",
            OperationId = "DeleteDepartment"
        )]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
                if (department == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Department not found"
                    });
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole == "Admin")
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);
                    if (admin == null || admin.FacultyId != department.FacultyId)
                    {
                        return Forbid("You can only delete departments from your faculty");
                    }
                }

                Expression<Func<Department, object>>[] includes = { d => d.Courses };
                var departmentWithCourses = await _unitOfWork.Repository<Department>().GetByIdAsync(id, includes);

                if (departmentWithCourses?.Courses?.Any() == true)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Cannot delete department that has courses. Please delete the courses first."
                    });
                }

                await _unitOfWork.Repository<Department>().DeleteAsync(department);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Department with ID {DepartmentId} soft deleted", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Department deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete department with ID {DepartmentId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while deleting the department"
                });
            }
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Search departments",
            Description = "Search departments by name. Admin can only search in their faculty.",
            OperationId = "SearchDepartments"
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepartmentDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchDepartments([FromQuery] string? searchTerm)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                Expression<Func<Department, bool>> filter;
                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                if (currentUserRole == "SuperAdmin")
                {
                    filter = d => true;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        filter = d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm);
                    }
                }
                else if (currentUserRole == "Admin")
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);
                    if (admin == null)
                    {
                        return Unauthorized(new ApiErrorResponse
                        {
                            Success = false,
                            Error = "Admin not found"
                        });
                    }

                    filter = d => d.FacultyId == admin.FacultyId;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        filter = d => d.FacultyId == admin.FacultyId &&
                                     (d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm));
                    }
                }
                else
                {
                    return Forbid("You don't have permission to search departments");
                }

                var departments = await _unitOfWork.Repository<Department>().FindAsync(filter, includes);
                var departmentDtos = _mapper.Map<IEnumerable<DepartmentDto>>(departments);

                return Ok(new ApiResponse<IEnumerable<DepartmentDto>>
                {
                    Success = true,
                    Message = "Departments search completed successfully",
                    Data = departmentDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search departments");
                return BadRequest(new ApiErrorResponse { Success = false, Error = "An error occurred while searching departments" });
            }
        }

        [HttpGet("my-faculty")]
        [SwaggerOperation(
            Summary = "Get departments for current admin's faculty",
            Description = "Retrieves all departments for the current admin's faculty",
            OperationId = "GetMyFacultyDepartments"
        )]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepartmentDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyFacultyDepartments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(currentUserId);

                if (admin == null)
                {
                    return Unauthorized(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Admin not found"
                    });
                }

                Expression<Func<Department, object>>[] includes =
                {
                    d => d.Faculty,
                    d => d.Faculty.University,
                    d => d.Courses
                };

                var departments = await _unitOfWork.Repository<Department>()
                    .FindAsync(d => d.FacultyId == admin.FacultyId, includes);

                var departmentDtos = _mapper.Map<IEnumerable<DepartmentDto>>(departments);

                return Ok(new ApiResponse<IEnumerable<DepartmentDto>>
                {
                    Success = true,
                    Message = $"Departments for your faculty retrieved successfully",
                    Data = departmentDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve departments for admin {AdminId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse { Success = false, Error = "An error occurred while retrieving departments" });
            }
        }

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        private Dictionary<string, string[]> GetModelStateErrors()
        {
            return ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
        }

        #endregion
    }
}
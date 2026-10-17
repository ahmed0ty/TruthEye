using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq.Expressions;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Swagger;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]
    public class FacultyController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FacultyController> _logger;
        private readonly IMapper _mapper;

        public FacultyController(IUnitOfWork unitOfWork, ILogger<FacultyController> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all faculties",
            Description = "Retrieves all faculties with their university and departments count",
            OperationId = "GetAllFaculties"
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FacultyDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllFaculties()
        {
            try
            {
                Expression<Func<Faculty, object>>[] includes =
                {
                    f => f.University,
                    f => f.Departments
                };

                var faculties = await _unitOfWork.Repository<Faculty>().GetAllAsync(includes);
                var facultyDtos = _mapper.Map<IEnumerable<FacultyDto>>(faculties);

                return Ok(new ApiResponse<IEnumerable<FacultyDto>>
                {
                    Success = true,
                    Message = "Faculties retrieved successfully",
                    Data = facultyDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve faculties");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving faculties"
                });
            }
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Get faculty by ID",
            Description = "Retrieves a specific faculty by ID with university and departments details",
            OperationId = "GetFacultyById"
        )]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFacultyById(int id)
        {
            try
            {
                Expression<Func<Faculty, object>>[] includes =
                {
                    f => f.University,
                    f => f.Departments
                };

                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(id, includes);

                if (faculty == null)
                {
                    _logger.LogWarning("Faculty with ID {FacultyId} not found", id);
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty not found"
                    });
                }

                var facultyDto = _mapper.Map<FacultyDto>(faculty);
                return Ok(new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty retrieved successfully",
                    Data = facultyDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve faculty with ID {FacultyId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving the faculty"
                });
            }
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Create new faculty",
            Description = "Creates a new faculty for a university",
            OperationId = "CreateFaculty"
        )]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFaculty([FromBody] CreateFacultyDto createFacultyDto)
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

                var university = await _unitOfWork.Repository<University>().GetByIdAsync(createFacultyDto.UniversityId);
                if (university == null)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University not found"
                    });
                }

                var existingFaculties = await _unitOfWork.Repository<Faculty>()
                    .FindAsync(f => f.Name.ToLower() == createFacultyDto.Name.ToLower()
                                 && f.UniversityId == createFacultyDto.UniversityId);

                if (existingFaculties.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty with this name already exists in the selected university"
                    });
                }

                var faculty = _mapper.Map<Faculty>(createFacultyDto);

                await _unitOfWork.Repository<Faculty>().AddAsync(faculty);
                await _unitOfWork.SaveChangesAsync();

                Expression<Func<Faculty, object>>[] includes =
                {
                    f => f.University,
                    f => f.Departments
                };

                var createdFaculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(faculty.Id, includes);
                var facultyDto = _mapper.Map<FacultyDto>(createdFaculty);

                _logger.LogInformation("Faculty created with ID {FacultyId}: {FacultyName} in university {UniversityId}",
                    faculty.Id, faculty.Name, faculty.UniversityId);

                return CreatedAtAction(nameof(GetFacultyById), new { id = faculty.Id }, new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty created successfully",
                    Data = facultyDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create faculty");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while creating the faculty"
                });
            }
        }

        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update faculty",
            Description = "Updates an existing faculty's information",
            OperationId = "UpdateFaculty"
        )]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFaculty(int id, [FromBody] UpdateFacultyDto updateFacultyDto)
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

                var existingFaculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(id);
                if (existingFaculty == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty not found"
                    });
                }

                // Check if university exists
                var university = await _unitOfWork.Repository<University>().GetByIdAsync(updateFacultyDto.UniversityId);
                if (university == null)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University not found"
                    });
                }

                // Check if another faculty with the same name exists in the same university (excluding current one)
                var duplicateFaculties = await _unitOfWork.Repository<Faculty>()
                    .FindAsync(f => f.Name.ToLower() == updateFacultyDto.Name.ToLower()
                                 && f.UniversityId == updateFacultyDto.UniversityId
                                 && f.Id != id);

                if (duplicateFaculties.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Another faculty with this name already exists in the selected university"
                    });
                }

                // Manual mapping to update properties
                existingFaculty.Name = updateFacultyDto.Name;
                existingFaculty.UniversityId = updateFacultyDto.UniversityId;

                await _unitOfWork.Repository<Faculty>().UpdateAsync(existingFaculty);
                await _unitOfWork.SaveChangesAsync();

                // الحصول على البيانات المحدثة مع العلاقات
                Expression<Func<Faculty, object>>[] includes =
                {
                    f => f.University,
                    f => f.Departments
                };

                var updatedFaculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(id, includes);
                var facultyDto = _mapper.Map<FacultyDto>(updatedFaculty);

                _logger.LogInformation("Faculty with ID {FacultyId} updated", id);

                return Ok(new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty updated successfully",
                    Data = facultyDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update faculty with ID {FacultyId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while updating the faculty"
                });
            }
        }

        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete faculty",
            Description = "Soft deletes a faculty",
            OperationId = "DeleteFaculty"
        )]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteFaculty(int id)
        {
            try
            {
                var faculty = await _unitOfWork.Repository<Faculty>().GetByIdAsync(id);
                if (faculty == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Faculty not found"
                    });
                }

                // Check if faculty has departments before deletion
                Expression<Func<Faculty, object>>[] includes = { f => f.Departments };
                var facultyWithDepartments = await _unitOfWork.Repository<Faculty>().GetByIdAsync(id, includes);

                if (facultyWithDepartments?.Departments?.Any() == true)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Cannot delete faculty that has departments. Please delete the departments first."
                    });
                }

                await _unitOfWork.Repository<Faculty>().DeleteAsync(faculty);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Faculty with ID {FacultyId} soft deleted", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Faculty deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete faculty with ID {FacultyId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while deleting the faculty"
                });
            }
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Search faculties",
            Description = "Search faculties by name or university",
            OperationId = "SearchFaculties"
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FacultyDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchFaculties(
            [FromQuery] string? searchTerm,
            [FromQuery] int? universityId)
        {
            try
            {
                Expression<Func<Faculty, bool>> filter = f => true;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filter = f => f.Name.Contains(searchTerm);
                }

                if (universityId.HasValue)
                {
                    var originalFilter = filter;
                    filter = f => originalFilter.Compile()(f) && f.UniversityId == universityId.Value;
                }

                Expression<Func<Faculty, object>>[] includes =
                {
                    f => f.University,
                    f => f.Departments
                };

                var faculties = await _unitOfWork.Repository<Faculty>().FindAsync(filter, includes);
                var facultyDtos = _mapper.Map<IEnumerable<FacultyDto>>(faculties);

                return Ok(new ApiResponse<IEnumerable<FacultyDto>>
                {
                    Success = true,
                    Message = "Faculties search completed successfully",
                    Data = facultyDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search faculties");
                return BadRequest(new ApiErrorResponse { Success = false, Error = "An error occurred while searching faculties" });
            }
        }

      
        #region Private Methods

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

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
    //[Authorize]
    [Produces("application/json")]
    public class UniversityController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UniversityController> _logger; 
        private readonly IMapper _mapper;

        public UniversityController(IUnitOfWork unitOfWork, ILogger<UniversityController> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all universities",
            Description = "Retrieves all universities with their faculties count",
            OperationId = "GetAllUniversities"
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UniversityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllUniversities()
        {
            try
            {
                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var universities = await _unitOfWork.Repository<University>().GetAllAsync(includes);

                var universityDtos = _mapper.Map<IEnumerable<UniversityDto>>(universities);

                return Ok(new ApiResponse<IEnumerable<UniversityDto>>
                {
                    Success = true,
                    Message = "Universities retrieved successfully",
                    Data = universityDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve universities");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving universities"
                });
            }
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Get university by ID",
            Description = "Retrieves a specific university by ID with faculties details",
            OperationId = "GetUniversityById"
        )]
        [ProducesResponseType(typeof(ApiResponse<UniversityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUniversityById(int id)
        {
            try
            {
                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var university = await _unitOfWork.Repository<University>().GetByIdAsync(id, includes);

                if (university == null)
                {
                    _logger.LogWarning("University with ID {UniversityId} not found", id);
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University not found"
                    });
                }

                var universityDto = _mapper.Map<UniversityDto>(university);
                return Ok(new ApiResponse<UniversityDto>
                {
                    Success = true,
                    Message = "University retrieved successfully",
                    Data = universityDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve university with ID {UniversityId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while retrieving the university"
                });
            }
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Create new university",
            Description = "Creates a new university with location details",
            OperationId = "CreateUniversity"
        )]
        [ProducesResponseType(typeof(ApiResponse<UniversityDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUniversity([FromBody] CreateUniversityDto createUniversityDto)
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

                var existingUniversities = await _unitOfWork.Repository<University>()
                    .FindAsync(u => u.Name.ToLower() == createUniversityDto.Name.ToLower());

                if (existingUniversities.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University with this name already exists"
                    });
                }

                var university = _mapper.Map<University>(createUniversityDto);
                university.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<University>().AddAsync(university);
                await _unitOfWork.SaveChangesAsync();

                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var createdUniversity = await _unitOfWork.Repository<University>().GetByIdAsync(university.Id, includes);

                var universityDto = _mapper.Map<UniversityDto>(createdUniversity);

                _logger.LogInformation("University created with ID {UniversityId}: {UniversityName}", university.Id, university.Name);

                return CreatedAtAction(nameof(GetUniversityById), new { id = university.Id }, new ApiResponse<UniversityDto>
                {
                    Success = true,
                    Message = "University created successfully",
                    Data = universityDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create university");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while creating the university"
                });
            }
        }

        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update university",
            Description = "Updates an existing university's information",
            OperationId = "UpdateUniversity"
        )]
        [ProducesResponseType(typeof(ApiResponse<UniversityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUniversity(int id, [FromBody] UpdateUniversityDto updateUniversityDto)
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

                var existingUniversity = await _unitOfWork.Repository<University>().GetByIdAsync(id);
                if (existingUniversity == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University not found"
                    });
                }

                var duplicateUniversities = await _unitOfWork.Repository<University>()
                    .FindAsync(u => u.Name.ToLower() == updateUniversityDto.Name.ToLower() && u.Id != id);

                if (duplicateUniversities.Any())
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Another university with this name already exists"
                    });
                }

                existingUniversity.Name = updateUniversityDto.Name;
                existingUniversity.Location = updateUniversityDto.Location;
                existingUniversity.City = updateUniversityDto.City;
                existingUniversity.Country = updateUniversityDto.Country;

                await _unitOfWork.Repository<University>().UpdateAsync(existingUniversity);
                await _unitOfWork.SaveChangesAsync();

                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var updatedUniversity = await _unitOfWork.Repository<University>().GetByIdAsync(id, includes);

                var universityDto = _mapper.Map<UniversityDto>(updatedUniversity);

                _logger.LogInformation("University with ID {UniversityId} updated", id);

                return Ok(new ApiResponse<UniversityDto>
                {
                    Success = true,
                    Message = "University updated successfully",
                    Data = universityDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update university with ID {UniversityId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while updating the university"
                });
            }
        }

        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete university",
            Description = "Soft deletes a university",
            OperationId = "DeleteUniversity"
        )]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUniversity(int id)
        {
            try
            {
                var university = await _unitOfWork.Repository<University>().GetByIdAsync(id);
                if (university == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "University not found"
                    });
                }

                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var universityWithFaculties = await _unitOfWork.Repository<University>().GetByIdAsync(id, includes);

                if (universityWithFaculties?.Faculties?.Any() == true)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Cannot delete university that has faculties. Please delete the faculties first."
                    });
                }

                await _unitOfWork.Repository<University>().DeleteAsync(university);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("University with ID {UniversityId} soft deleted", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "University deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete university with ID {UniversityId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "An error occurred while deleting the university"
                });
            }
        }

        [HttpGet("search")]
        [SwaggerOperation(
             Summary = "Search universities",
             Description = "Search universities by name, city, or country",
             OperationId = "SearchUniversities"
         )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UniversityDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUniversities(
             [FromQuery] string? searchTerm,
             [FromQuery] string? city,
             [FromQuery] string? country)
        {
            try
            {
                Expression<Func<University, bool>> filter = u => true;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filter = u => u.Name.Contains(searchTerm) || u.Location.Contains(searchTerm);
                }

                if (!string.IsNullOrEmpty(city))
                {
                    var originalFilter = filter;
                    filter = u => originalFilter.Compile()(u) && u.City.ToLower() == city.ToLower();
                }

                if (!string.IsNullOrEmpty(country))
                {
                    var originalFilter = filter;
                    filter = u => originalFilter.Compile()(u) && u.Country.ToLower() == country.ToLower();
                }

                Expression<Func<University, object>>[] includes = { u => u.Faculties };
                var universities = await _unitOfWork.Repository<University>().FindAsync(filter, includes);
                var universityDtos = _mapper.Map<IEnumerable<UniversityDto>>(universities);

                return Ok(new ApiResponse<IEnumerable<UniversityDto>>
                {
                    Success = true,
                    Message = "Universities search completed successfully",
                    Data = universityDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search universities");
                return BadRequest(new ApiErrorResponse { Success = false, Error = "An error occurred while searching universities" });
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
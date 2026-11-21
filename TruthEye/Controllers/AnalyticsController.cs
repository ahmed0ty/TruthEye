using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Middleware;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.Swagger;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _svc;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IAnalyticsService svc, ILogger<AnalyticsController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private IActionResult Done<T>(T data) => Ok(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        private IActionResult Fail(Exception ex) => ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        [HttpGet("exam/{examId:int}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Exam(int examId)
        {
            try { return Done(await _svc.ExamAsync(Me, examId)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("faculty")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> Faculty([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try { return Done(await _svc.FacultyAsync(Me, from, to)); } catch (Exception ex) { return Fail(ex); }
        }
    }
}
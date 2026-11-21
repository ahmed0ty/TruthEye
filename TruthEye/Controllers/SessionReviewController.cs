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
    [Authorize(Roles = "Doctor,Proctor," + AppRoles.FacultyStaff)]
    public class SessionReviewController : ControllerBase
    {
        private readonly ISessionReviewService _svc;
        private readonly ILogger<SessionReviewController> _logger;

        public SessionReviewController(ISessionReviewService svc, ILogger<SessionReviewController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private IActionResult Ok2<T>(T data) => Ok(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        private IActionResult Fail(Exception ex) => ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ReviewFilter f)
        {
            try { return Ok2(await _svc.ListAsync(Me, f)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("{sessionId:int}")]
        public async Task<IActionResult> Get(int sessionId)
        {
            try { return Ok2(await _svc.GetAsync(Me, sessionId)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("{sessionId:int}/decision")]
        [Authorize(Roles = "Doctor,Proctor," + AppRoles.Managers)]
        public async Task<IActionResult> Decide(int sessionId, [FromBody] ReviewDecisionDto dto)
        {
            try { return Ok2(await _svc.DecideAsync(Me, sessionId, dto)); } catch (Exception ex) { return Fail(ex); }
        }
    }
}
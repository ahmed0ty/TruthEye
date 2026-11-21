using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Middleware;
using TruthEye.Services;
using TruthEye.Swagger;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class GradingController : ControllerBase
    {
        private readonly IGradingService _svc;
        private readonly ILogger<GradingController> _logger;

        public GradingController(IGradingService svc, ILogger<GradingController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private IActionResult Done<T>(T data) => Ok(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        private IActionResult Fail(Exception ex) => ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        [HttpGet("session/{sessionId:int}")]
        public async Task<IActionResult> Session(int sessionId)
        {
            try { return Done(await _svc.GetSessionAsync(Me, sessionId)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("answers/{answerId:int}")]
        public async Task<IActionResult> Adjust(int answerId, [FromBody] AdjustGradeDto dto)
        {
            try { return Done(await _svc.AdjustAsync(Me, answerId, dto)); } catch (Exception ex) { return Fail(ex); }
        }
    }
}
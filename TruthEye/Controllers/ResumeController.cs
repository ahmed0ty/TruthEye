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
    [Authorize(Roles = "Student,Doctor,Proctor")]
    public class ResumeController : ControllerBase
    {
        private readonly IResumeService _svc;
        private readonly ILogger<ResumeController> _logger;
        public ResumeController(IResumeService svc, ILogger<ResumeController> logger) { _svc = svc; _logger = logger; }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private IActionResult Done<T>(T data) => Ok(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        private IActionResult Fail(Exception ex) => ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        [HttpGet("status/{sessionId:int}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Status(int sessionId)
        { try { return Done(await _svc.StatusAsync(Me, sessionId)); } catch (Exception ex) { return Fail(ex); } }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Request([FromBody] CreateResumeDto dto)
        { try { return Done(await _svc.RequestAsync(Me, dto)); } catch (Exception ex) { return Fail(ex); } }

        [HttpGet("pending")]
        [Authorize(Roles = "Doctor,Proctor")]
        public async Task<IActionResult> Pending()
        { try { return Done(await _svc.PendingAsync(Me)); } catch (Exception ex) { return Fail(ex); } }

        [HttpPost("{id:int}/decision")]
        [Authorize(Roles = "Doctor,Proctor")]
        public async Task<IActionResult> Decide(int id, [FromBody] ResumeDecisionDto dto)
        { try { return Done(await _svc.DecideAsync(Me, id, dto)); } catch (Exception ex) { return Fail(ex); } }
    }
}
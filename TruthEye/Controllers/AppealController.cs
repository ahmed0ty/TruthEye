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
    [Authorize(Roles = "Student,Doctor")]
    public class AppealController : ControllerBase
    {
        private readonly IAppealService _svc;
        private readonly ILogger<AppealController> _logger;

        public AppealController(IAppealService svc, ILogger<AppealController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private IActionResult Done<T>(T data) => Ok(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        private IActionResult Fail(Exception ex) => ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Mine()
        {
            try { return Done(await _svc.MineAsync(Me)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([FromBody] CreateAppealDto dto)
        {
            try { return Done(await _svc.CreateAsync(Me, dto)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try { return Done(await _svc.ListAsync(Me, status, page, pageSize)); } catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("{id:int}/respond")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Respond(int id, [FromBody] AppealResponseDto dto)
        {
            try { return Done(await _svc.RespondAsync(Me, id, dto)); } catch (Exception ex) { return Fail(ex); }
        }
    }
}
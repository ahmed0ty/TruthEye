using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settings;

        public SettingsController(ISettingsService settings) => _settings = settings;

        // Read by the exam screen when a student starts
        [HttpGet("proctoring")]
        public async Task<IActionResult> Proctoring()
        {
            var data = new
            {
                noFaceSeconds = await _settings.GetIntAsync(SettingKeys.NoFaceSeconds, 3, 2, 30),
                multipleFacesSeconds = await _settings.GetIntAsync(SettingKeys.MultipleFacesSeconds, 2, 2, 30)
            };
            return Ok(new { success = true, data });
        }
    }
}
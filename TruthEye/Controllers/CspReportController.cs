using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TruthEye.Controllers
{
    /// يستقبل تقارير انتهاكات CSP من المتصفح (وضع report-only). بيسجلها في اللوج بس، مايحفظهاش في الداتابيز.
    [ApiController]
    [Route("api/csp-report")]
    public class CspReportController : ControllerBase
    {
        private const int MaxBodyBytes = 8 * 1024;
        private const int MaxLoggedChars = 2000;

        private readonly ILogger<CspReportController> _logger;

        public CspReportController(ILogger<CspReportController> logger)
        {
            _logger = logger;
        }

        // المتصفح بيبعت التقرير من غير توكن، فالـ endpoint مفتوح. الـ rate limit والحد الأقصى للحجم بيمنعوا إغراق اللوج.
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<IActionResult> Receive()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var logged = body.Length > MaxLoggedChars ? body[..MaxLoggedChars] : body;
            _logger.LogWarning("CSP violation report: {Report}", logged);

            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // The student may only export reports that belong to them
        private async Task<bool> StudentOwnsReport(int reportId)
        {
            var mine = await _reportService.GetStudentReportsAsync(CurrentUserId());
            return mine.Any(x => x.Id == reportId);
        }

        [Authorize(Roles = "Student,Doctor,Admin")]
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromQuery] int studentId, [FromQuery] int examId)
        {
            try
            {
                // A student can only generate a report for themselves
                if (User.IsInRole("Student"))
                    studentId = CurrentUserId();

                var report = await _reportService.GeneratePerformanceReportAsync(studentId, examId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("student")]
        public async Task<IActionResult> GetStudentReports()
        {
            try
            {
                var reports = await _reportService.GetStudentReportsAsync(CurrentUserId());
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve student reports");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student,Doctor,Admin")]
        [HttpGet("{reportId}/pdf")]
        public async Task<IActionResult> ExportToPdf(int reportId)
        {
            try
            {
                if (User.IsInRole("Student") && !await StudentOwnsReport(reportId))
                    return Forbid();

                var pdfBytes = await _reportService.ExportReportToPdfAsync(reportId);
                return File(pdfBytes, "application/pdf", $"report_{reportId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export report to PDF: {ReportId}", reportId);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student,Doctor,Admin")]
        [HttpGet("{reportId}/excel")]
        public async Task<IActionResult> ExportToExcel(int reportId)
        {
            try
            {
                if (User.IsInRole("Student") && !await StudentOwnsReport(reportId))
                    return Forbid();

                var excelBytes = await _reportService.ExportReportToExcelAsync(reportId);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"report_{reportId}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export report to Excel: {ReportId}", reportId);
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
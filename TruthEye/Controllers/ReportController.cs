using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Middleware;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, IUnitOfWork uow, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _uow = uow;
            _logger = logger;
        }

        private int CurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string Role => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        private async Task<bool> CanAccess(int studentId, int examId)
        {
            var me = CurrentUserId();

            if (Role == "Student") return studentId == me;

            if (Role == "Doctor")
            {
                var exam = await _uow.Repository<Exam>().GetByIdAsync(examId);
                return exam != null && exam.DoctorId == me;
            }

            var staff = await _uow.Repository<Admin>().GetByIdAsync(me);
            var student = await _uow.Repository<Student>().GetByIdAsync(studentId);
            return staff is { IsActive: true } && student != null && student.FacultyId == staff.FacultyId;
        }

        private async Task<PerformanceReport?> AccessibleReport(int reportId)
        {
            var r = await _uow.Repository<PerformanceReport>().GetByIdAsync(reportId);
            return r != null && await CanAccess(r.StudentId, r.ExamId) ? r : null;
        }

        [Authorize(Roles = "Student,Doctor," + AppRoles.FacultyStaff)]
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromQuery] int studentId, [FromQuery] int examId)
        {
            try
            {
                if (Role == "Student") studentId = CurrentUserId();

                if (!await CanAccess(studentId, examId))
                    return NotFound(new { Error = "Not found" });

                return Ok(await _reportService.GeneratePerformanceReportAsync(studentId, examId));
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GenerateReport)); }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("student")]
        public async Task<IActionResult> GetStudentReports()
        {
            try { return Ok(await _reportService.GetStudentReportsAsync(CurrentUserId())); }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetStudentReports)); }
        }

        [Authorize(Roles = "Student,Doctor," + AppRoles.FacultyStaff)]
        [HttpGet("{reportId}/pdf")]
        public async Task<IActionResult> ExportToPdf(int reportId)
        {
            try
            {
                if (await AccessibleReport(reportId) == null) return NotFound(new { Error = "Report not found" });

                var bytes = await _reportService.ExportReportToPdfAsync(reportId);
                return File(bytes, "application/pdf", $"report_{reportId}.pdf");
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(ExportToPdf)); }
        }

        [Authorize(Roles = "Student,Doctor," + AppRoles.FacultyStaff)]
        [HttpGet("{reportId}/excel")]
        public async Task<IActionResult> ExportToExcel(int reportId)
        {
            try
            {
                if (await AccessibleReport(reportId) == null) return NotFound(new { Error = "Report not found" });

                var bytes = await _reportService.ExportReportToExcelAsync(reportId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"report_{reportId}.xlsx");
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(ExportToExcel)); }
        }
    }
}
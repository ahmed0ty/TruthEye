using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface IReportService
    {
        Task<PerformanceReport> GeneratePerformanceReportAsync(int studentId, int examId);
        Task<IEnumerable<PerformanceReport>> GetStudentReportsAsync(int studentId);
        Task<byte[]> ExportReportToPdfAsync(int reportId);
        Task<byte[]> ExportReportToExcelAsync(int reportId);
    }
}

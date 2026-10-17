using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<PerformanceReport> GeneratePerformanceReportAsync(int studentId, int examId)
        {
            var sessions = (await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.StudentId == studentId && es.ExamId == examId && es.Status != "InProgress")).ToList();

            if (!sessions.Any())
                throw new Exception("No completed exam session found");

            var latest = sessions.OrderByDescending(s => s.StartTime).First();

            var aiLogs = await _unitOfWork.Repository<AILog>()
                .FindAsync(log => log.ExamSessionId == latest.Id);

            var report = new PerformanceReport
            {
                StudentId = studentId,
                ExamId = examId,
                Score = latest.Percentage,
                AIAlertsCount = aiLogs.Count(),
                Duration = latest.EndTime.HasValue ? latest.EndTime.Value - latest.StartTime : TimeSpan.Zero,
                GeneratedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PerformanceReport>().AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return report;
        }

        public async Task<IEnumerable<PerformanceReport>> GetStudentReportsAsync(int studentId)
        {
            return await _unitOfWork.Repository<PerformanceReport>()
                .FindAsync(r => r.StudentId == studentId, r => r.Exam);
        }

        private async Task<(PerformanceReport r, string exam, string student)> Load(int reportId)
        {
            var r = await _unitOfWork.Repository<PerformanceReport>()
                .GetByIdAsync(reportId, x => x.Exam, x => x.Student);
            if (r == null) throw new Exception("Report not found");
            return (r, r.Exam?.Title ?? $"Exam #{r.ExamId}",
                r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : $"Student #{r.StudentId}");
        }

        public async Task<byte[]> ExportReportToPdfAsync(int reportId)
        {
            var (r, exam, student) = await Load(reportId);

            return Document.Create(c => c.Page(p =>
            {
                p.Margin(40);
                p.Header().Text("Performance Report").FontSize(24).Bold();
                p.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text($"Student: {student}");
                    col.Item().Text($"Exam: {exam}");
                    col.Item().Text($"Score: {r.Score:0.#}%");
                    col.Item().Text($"Proctoring alerts: {r.AIAlertsCount}");
                    col.Item().Text($"Duration: {r.Duration:hh\\:mm\\:ss}");
                    col.Item().Text($"Generated: {r.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
                });
            })).GeneratePdf();
        }

        public async Task<byte[]> ExportReportToExcelAsync(int reportId)
        {
            var (r, exam, student) = await Load(reportId);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Report");
            var rows = new (string, object)[]
            {
                ("Student", student),
                ("Exam", exam),
                ("Score (%)", Math.Round(r.Score, 1)),
                ("Proctoring alerts", r.AIAlertsCount),
                ("Duration", r.Duration.ToString(@"hh\:mm\:ss")),
                ("Generated (UTC)", r.GeneratedAt.ToString("yyyy-MM-dd HH:mm")),
            };
            for (var i = 0; i < rows.Length; i++)
            {
                ws.Cell(i + 1, 1).Value = rows[i].Item1;
                ws.Cell(i + 1, 1).Style.Font.Bold = true;
                ws.Cell(i + 1, 2).Value = XLCellValue.FromObject(rows[i].Item2);
            }
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
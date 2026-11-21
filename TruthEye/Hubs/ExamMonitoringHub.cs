// // using Microsoft.AspNetCore.Authorization;
// // using Microsoft.AspNetCore.SignalR;
// // using System.Security.Claims;

// // namespace TruthEye.API.Hubs
// // {
// //     [Authorize]
// //     public class ExamMonitoringHub : Hub
// //     {
// //         private readonly ILogger<ExamMonitoringHub> _logger;

// //         public ExamMonitoringHub(ILogger<ExamMonitoringHub> logger)
// //         {
// //             _logger = logger;
// //         }

// //         public override async Task OnConnectedAsync()
// //         {
// //             var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
// //             var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

// //             _logger.LogInformation("User connected to exam monitoring: {UserId} - Role: {Role}", userId, userRole);
// //             await base.OnConnectedAsync();
// //         }

// //         public async Task JoinExamMonitoring(string examSessionId)
// //         {
// //             await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{examSessionId}");
// //             _logger.LogInformation("Student joined exam monitoring: {SessionId}", examSessionId);
// //         }

// //         [Authorize(Roles = "Proctor,Admin")]
// //         public async Task MonitorExam(string examId)
// //         {
// //             await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-monitor:{examId}");
// //             _logger.LogInformation("Proctor monitoring exam: {ExamId}", examId);
// //         }

// //         public async Task SendAIAlert(string examSessionId, string eventType, double confidence)
// //         {
// //             var alert = new
// //             {
// //                 sessionId = examSessionId,
// //                 eventType,
// //                 confidence,
// //                 timestamp = DateTime.UtcNow
// //             };

// //             await Clients.Group($"exam-monitor:{examSessionId}").SendAsync("AIAlertReceived", alert);

// //             _logger.LogWarning("AI Alert: {EventType} - Confidence: {Confidence} - Session: {SessionId}",
// //                 eventType, confidence, examSessionId);
// //         }

// //         public async Task UpdateStudentStatus(string examSessionId, string status)
// //         {
// //             await Clients.Group($"exam-monitor:{examSessionId}").SendAsync("StudentStatusChanged", new
// //             {
// //                 sessionId = examSessionId,
// //                 status,
// //                 timestamp = DateTime.UtcNow
// //             });
// //         }
// //     }
// // }



// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.SignalR;
// using System.Security.Claims;

// namespace TruthEye.API.Hubs
// {
//     [Authorize]
//     public class ExamMonitoringHub : Hub
//     {
//         private readonly ILogger<ExamMonitoringHub> _logger;

//         public ExamMonitoringHub(ILogger<ExamMonitoringHub> logger)
//         {
//             _logger = logger;
//         }

//         public override async Task OnConnectedAsync()
//         {
//             var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//             var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

//             _logger.LogInformation("User connected to exam monitoring: {UserId} - Role: {Role}", userId, userRole);
//             await base.OnConnectedAsync();
//         }

//         public async Task JoinExamMonitoring(string examSessionId)
//         {
//             await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{examSessionId}");
//             _logger.LogInformation("Student joined exam monitoring: {SessionId}", examSessionId);
//         }

//         [Authorize(Roles = "Proctor,Admin")]
//         public async Task MonitorExam(string examId)
//         {
//             await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-monitor:{examId}");
//             _logger.LogInformation("Proctor monitoring exam: {ExamId}", examId);
//         }

//         // ===== الدالتين المعدلتين =====

//         public async Task SendAIAlert(string examId, string examSessionId, string eventType, double confidence)
//         {
//             var alert = new { sessionId = examSessionId, examId, eventType, confidence, timestamp = DateTime.UtcNow };
//             await Clients.Group($"exam-monitor:{examId}").SendAsync("AIAlertReceived", alert);

//             _logger.LogWarning("AI Alert: {EventType} - Confidence: {Confidence} - Session: {SessionId}",
//                 eventType, confidence, examSessionId);
//         }

//         public async Task UpdateStudentStatus(string examId, string examSessionId, string status)
//         {
//             await Clients.Group($"exam-monitor:{examId}").SendAsync("StudentStatusChanged", new
//             {
//                 sessionId = examSessionId, examId, status, timestamp = DateTime.UtcNow
//             });
//         }
//     }
// }
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.UnitOfWorkk;

namespace TruthEye.API.Hubs
{
    [Authorize]
    public class ExamMonitoringHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExamMonitoringHub> _logger;

        public ExamMonitoringHub(IUnitOfWork unitOfWork, ILogger<ExamMonitoringHub> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private int Me => int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string Role => Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected to exam monitoring: {UserId} - Role: {Role}", Me, Role);
            await base.OnConnectedAsync();
        }

        // الطالب لازم يكون صاحب الجلسة وهي شغالة
        private async Task<ExamSession?> OwnSessionAsync(string examSessionId)
        {
            if (!int.TryParse(examSessionId, out var sid)) return null;
            var s = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sid);
            return s != null && s.StudentId == Me && s.Status == "InProgress" ? s : null;
        }

        [Authorize(Roles = "Student")]
        public async Task JoinExamMonitoring(string examSessionId)
        {
            if (await OwnSessionAsync(examSessionId) == null) throw new HubException("Access denied");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{examSessionId}");
        }

        [Authorize(Roles = "Proctor," + AppRoles.FacultyStaff)]
        public async Task MonitorExam(string examId)
        {
            if (!int.TryParse(examId, out var eid)) throw new HubException("Invalid exam");

            var exam = await _unitOfWork.Repository<Exam>()
                .GetByIdAsync(eid, e => e.Course, e => e.Course.Department);
            if (exam?.Course?.Department == null) throw new HubException("Exam not found");

            int? faculty = Role == "Proctor"
                ? (await _unitOfWork.Repository<Proctor>().GetByIdAsync(Me))?.FacultyId
                : (await _unitOfWork.Repository<Admin>().GetByIdAsync(Me))?.FacultyId;

            if (faculty == null || faculty != exam.Course.Department.FacultyId)
                throw new HubException("Access denied");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-monitor:{examId}");
        }

        [Authorize(Roles = "Student")]
        public async Task SendAIAlert(string examId, string examSessionId, string eventType, double confidence)
        {
            var session = await OwnSessionAsync(examSessionId);
            // الـ examId بيتاخد من الجلسة نفسها، مش من اللي الطالب بعته
            if (session == null) throw new HubException("Access denied");

            var alert = new
            {
                sessionId = examSessionId,
                examId = session.ExamId.ToString(),
                eventType,
                confidence = Math.Clamp(confidence, 0, 1),
                timestamp = DateTime.UtcNow
            };
            await Clients.Group($"exam-monitor:{session.ExamId}").SendAsync("AIAlertReceived", alert);
        }

        [Authorize(Roles = "Student")]
        public async Task UpdateStudentStatus(string examId, string examSessionId, string status)
        {
            var session = await OwnSessionAsync(examSessionId);
            if (session == null) throw new HubException("Access denied");

            await Clients.Group($"exam-monitor:{session.ExamId}").SendAsync("StudentStatusChanged", new
            {
                sessionId = examSessionId,
                examId = session.ExamId.ToString(),
                status = status?.Length > 50 ? status[..50] : status,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
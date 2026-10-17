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

namespace TruthEye.API.Hubs
{
    [Authorize]
    public class ExamMonitoringHub : Hub
    {
        private readonly ILogger<ExamMonitoringHub> _logger;

        public ExamMonitoringHub(ILogger<ExamMonitoringHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("User connected to exam monitoring: {UserId} - Role: {Role}", userId, userRole);
            await base.OnConnectedAsync();
        }

        public async Task JoinExamMonitoring(string examSessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{examSessionId}");
            _logger.LogInformation("Student joined exam monitoring: {SessionId}", examSessionId);
        }

        [Authorize(Roles = "Proctor,Admin")]
        public async Task MonitorExam(string examId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-monitor:{examId}");
            _logger.LogInformation("Proctor monitoring exam: {ExamId}", examId);
        }

        [Authorize(Roles = "Student")]
        public async Task SendAIAlert(string examId, string examSessionId, string eventType, double confidence)
        {
            var alert = new { sessionId = examSessionId, examId, eventType, confidence, timestamp = DateTime.UtcNow };
            await Clients.Group($"exam-monitor:{examId}").SendAsync("AIAlertReceived", alert);

            _logger.LogWarning("AI Alert: {EventType} - Confidence: {Confidence} - Session: {SessionId}",
                eventType, confidence, examSessionId);
        }

        [Authorize(Roles = "Student")]
        public async Task UpdateStudentStatus(string examId, string examSessionId, string status)
        {
            await Clients.Group($"exam-monitor:{examId}").SendAsync("StudentStatusChanged", new
            {
                sessionId = examSessionId, examId, status, timestamp = DateTime.UtcNow
            });
        }
    }
}
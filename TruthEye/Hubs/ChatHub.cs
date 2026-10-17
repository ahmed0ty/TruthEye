using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IUnitOfWork unitOfWork, ILogger<ChatHub> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            _logger.LogInformation("User connected: {UserId} - {UserName}", userId, userName);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User disconnected: {UserId}", userId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            try
            {
                var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(senderIdClaim, out var senderId))
                {
                    await Clients.Caller.SendAsync("Error", "Unauthorized");
                    return;
                }

                var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

                if (!int.TryParse(receiverId, out var receiverIntId) || receiverIntId == senderId)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid recipient");
                    return;
                }

                var receiver = await _unitOfWork.Repository<User>().GetByIdAsync(receiverIntId);
                if (receiver == null || !receiver.IsActive)
                {
                    await Clients.Caller.SendAsync("Error", "Recipient not found");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid message");
                    return;
                }

                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverIntId,
                    MessageText = message,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<ChatMessage>().AddAsync(chatMessage);
                await _unitOfWork.SaveChangesAsync();

                await Clients.Group($"user:{receiverIntId}").SendAsync("ReceivePrivateMessage", new
                {
                    messageId = chatMessage.Id,
                    senderId = senderId.ToString(),
                    senderName,
                    message,
                    sentAt = chatMessage.SentAt
                });

                await Clients.Caller.SendAsync("MessageSent", new
                {
                    messageId = chatMessage.Id,
                    receiverId = receiverIntId.ToString(),
                    message,
                    sentAt = chatMessage.SentAt
                });

                _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", senderId, receiverIntId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending private message");
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        public async Task SendGroupMessage(string groupId, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId) ||
                    groupId.StartsWith("user:") ||
                    groupId.StartsWith("role:"))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid group");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid message");
                    return;
                }

                var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

                await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", new
                {
                    senderId,
                    senderName,
                    message,
                    sentAt = DateTime.UtcNow
                });

                _logger.LogInformation("Group message sent to {GroupId} by {SenderId}", groupId, senderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group message");
            }
        }

        public async Task JoinExamGroup(string examId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"exam:{examId}");
            _logger.LogInformation("User joined exam group: {ExamId}", examId);
        }

        public async Task LeaveExamGroup(string examId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exam:{examId}");
            _logger.LogInformation("User left exam group: {ExamId}", examId);
        }

        public async Task Typing(string receiverId)
        {
            if (!int.TryParse(receiverId, out var receiverIntId)) return;

            await Clients.Group($"user:{receiverIntId}").SendAsync("UserTyping",
                Context.User?.FindFirst(ClaimTypes.Name)?.Value);
        }
    }
}
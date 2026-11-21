using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatLockService _lock;

        public ChatHub(IUnitOfWork unitOfWork, ILogger<ChatHub> logger, IChatLockService chatLock)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _lock = chatLock;
        }

        private async Task<bool> IsLockedAsync(User a, User b) =>
            (await _lock.GetAsync(ChatRules.FacultyOf(a))).Locked
            || (await _lock.GetAsync(ChatRules.FacultyOf(b))).Locked;

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

                var sender = await _unitOfWork.Repository<User>().GetByIdAsync(senderId);
                if (sender == null || !sender.IsActive || !await ChatRules.CanChat(_unitOfWork, sender, receiver))
                {
                    await Clients.Caller.SendAsync("Error", "You can't message this user");
                    return;
                }

                if (await IsLockedAsync(sender, receiver))
                {
                    await Clients.Caller.SendAsync("Error", "Chat is closed while an exam is running");
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

        public async Task Typing(string receiverId)
        {
            if (!int.TryParse(receiverId, out var rid) || !int.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var me)) return;
            var a = await _unitOfWork.Repository<User>().GetByIdAsync(me);
            var b = await _unitOfWork.Repository<User>().GetByIdAsync(rid);
            if (a == null || b == null || !await ChatRules.CanChat(_unitOfWork, a, b)) return;
            if (await IsLockedAsync(a, b)) return;

            await Clients.Group($"user:{rid}").SendAsync("UserTyping", Context.User?.FindFirst(ClaimTypes.Name)?.Value);
        }
    }
}
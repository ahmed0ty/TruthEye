using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;
using TruthEye.Middleware;
namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatController> _logger;
        private readonly IChatLockService _lock;

        public ChatController(IUnitOfWork unitOfWork, ILogger<ChatController> logger, IChatLockService chatLock)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _lock = chatLock;
        }

        private async Task<bool> LockedAsync(int userId)
        {
            var me = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            return me != null && (await _lock.GetAsync(ChatRules.FacultyOf(me))).Locked;
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(int otherUserId, [FromQuery] int take = 50)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var msgs = await _unitOfWork.Repository<ChatMessage>().GetQueryable()
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Take(Math.Clamp(take, 1, 200))
                    .ToListAsync();

                var messages = msgs
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.MessageText,
                        m.SentAt,
                        m.IsRead,
                        m.SenderId,
                        m.ReceiverId,
                        IsMine = m.SenderId == currentUserId
                    })
                    .ToList();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to retrieve conversation");
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentConversations()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var query = _unitOfWork.Repository<ChatMessage>().GetQueryable()
                    .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId);

                // التجميع والترتيب والحد (20) بيتنفذوا في الداتا بيز
                var partners = await query
                    .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastSentAt = g.Max(m => m.SentAt),
                        UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                    })
                    .OrderByDescending(c => c.LastSentAt)
                    .Take(20)
                    .ToListAsync();

                var result = new List<object>();
                foreach (var p in partners)
                {
                    var partnerId = p.UserId;
                    var last = await query
                        .Where(m => m.SenderId == partnerId || m.ReceiverId == partnerId)
                        .OrderByDescending(m => m.SentAt)
                        .FirstAsync();

                    result.Add(new
                    {
                        p.UserId,
                        LastMessage = last,
                        p.UnreadCount
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to retrieve recent conversations");
            }
        }

        [HttpPut("mark-read/{otherUserId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int otherUserId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var unreadMessages = (await _unitOfWork.Repository<ChatMessage>()
                    .FindAsync(m => m.SenderId == otherUserId &&
                                   m.ReceiverId == currentUserId &&
                                   !m.IsRead)).ToList();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    await _unitOfWork.Repository<ChatMessage>().UpdateAsync(message);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Messages marked as read from {OtherUserId}", otherUserId);
                return Ok(new { Message = "Messages marked as read", Count = unreadMessages.Count });
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to mark messages as read");
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var message = await _unitOfWork.Repository<ChatMessage>().GetByIdAsync(messageId);

                if (message == null)
                    return NotFound(new { Error = "Message not found" });

                if (message.SenderId != currentUserId)
                    return Forbid();

                if (await LockedAsync(currentUserId))
                    return StatusCode(403, new { Error = "Chat is closed while an exam is running" });

                await _unitOfWork.Repository<ChatMessage>().DeleteAsync(message);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Message deleted: {MessageId}", messageId);
                return Ok(new { Message = "Message deleted" });
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to delete message");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string query, [FromQuery] int? userId = null)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                if (string.IsNullOrWhiteSpace(query) || query.Length > 100)
                    return BadRequest(new { Error = "Search text must be 1 to 100 characters" });

                // الفلترة والترتيب والحد بيتنفذوا في الداتا بيز، مش في الذاكرة
                var results = await _unitOfWork.Repository<ChatMessage>().GetQueryable()
                    .Where(m =>
                        (m.SenderId == currentUserId || m.ReceiverId == currentUserId) &&
                        m.MessageText.Contains(query) &&
                        (!userId.HasValue || m.SenderId == userId || m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .Select(m => new
                    {
                        m.Id,
                        m.MessageText,
                        m.SentAt,
                        m.SenderId,
                        m.ReceiverId,
                        IsMine = m.SenderId == currentUserId
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to search messages");
            }
        }
    }
}
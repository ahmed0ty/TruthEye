using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class ChatController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IUnitOfWork unitOfWork, ILogger<ChatController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(int otherUserId, [FromQuery] int take = 50)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var messages = (await _unitOfWork.Repository<ChatMessage>()
                    .FindAsync(m =>
                        m.SenderId == currentUserId && m.ReceiverId == otherUserId ||
                        m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Take(take)
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
                    });

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve conversation");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentConversations()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var messages = (await _unitOfWork.Repository<ChatMessage>()
                    .FindAsync(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId))
                    .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                        UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                    })
                    .OrderByDescending(c => c.LastMessage.SentAt)
                    .Take(20);

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recent conversations");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("mark-read/{otherUserId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int otherUserId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var unreadMessages = await _unitOfWork.Repository<ChatMessage>()
                    .FindAsync(m => m.SenderId == otherUserId &&
                                   m.ReceiverId == currentUserId &&
                                   !m.IsRead);

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    await _unitOfWork.Repository<ChatMessage>().UpdateAsync(message);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Messages marked as read from {OtherUserId}", otherUserId);
                return Ok(new { Message = "Messages marked as read", Count = unreadMessages.Count() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark messages as read");
                return BadRequest(new { Error = ex.Message });
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

                await _unitOfWork.Repository<ChatMessage>().DeleteAsync(message);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Message deleted: {MessageId}", messageId);
                return Ok(new { Message = "Message deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string query, [FromQuery] int? userId = null)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var messages = await _unitOfWork.Repository<ChatMessage>()
                    .FindAsync(m =>
                        (m.SenderId == currentUserId || m.ReceiverId == currentUserId) &&
                        m.MessageText.Contains(query) &&
                        (!userId.HasValue || m.SenderId == userId || m.ReceiverId == userId));

                var results = messages
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
                    });

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search messages");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}

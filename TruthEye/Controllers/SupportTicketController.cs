using TruthEye.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupportTicketController : ControllerBase
    {
        private readonly ISupportTicketService _ticketService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SupportTicketController> _logger;

        public SupportTicketController(ISupportTicketService ticketService, IUnitOfWork uow, ILogger<SupportTicketController> logger)
        {
            _ticketService = ticketService;
            _uow = uow;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketDto ticketDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var ticket = await _ticketService.CreateTicketAsync(userId, ticketDto);

                _logger.LogInformation("Support ticket created: {TicketId}", ticket.Id);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create support ticket");
                return ApiErrors.Result(ex, _logger, "CreateTicket");
            }
        }

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetMyTickets()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var tickets = await _ticketService.GetUserTicketsAsync(userId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user tickets");
                return ApiErrors.Result(ex, _logger, "GetMyTickets");
            }
        }

        [Authorize(Roles = "Support")]
        [HttpGet("assigned")]
        public async Task<IActionResult> GetAssignedTickets()
        {
            try
            {
                var supportId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var tickets = await _ticketService.GetAssignedTicketsAsync(supportId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve assigned tickets");
                return ApiErrors.Result(ex, _logger, "GetAssignedTickets");
            }
        }

        [Authorize(Roles = "Support," + AppRoles.FacultyStaff)]
        [HttpGet]
        public async Task<IActionResult> GetAllTickets([FromQuery] string? status = null)
        {
            try
            {
                int? facultyId = null;
                if (!User.IsInRole("Support"))
                {
                    var me = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                    var staff = await _uow.Repository<Admin>().GetByIdAsync(me);
                    if (staff is not { IsActive: true }) return Forbid();
                    facultyId = staff.FacultyId;
                }

                return Ok(await _ticketService.GetAllTicketsAsync(status, facultyId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all tickets");
                return BadRequest(new { Error = "Couldn't load tickets" });
            }
        }

        [Authorize(Roles = "Support")]
        [HttpPut("{ticketId}/assign/{supportId}")]
        public async Task<IActionResult> AssignTicket(int ticketId, int supportId)
        {
            try
            {
                var ticket = await _ticketService.AssignTicketAsync(ticketId, supportId);
                _logger.LogInformation("Ticket {TicketId} assigned to support {SupportId}", ticketId, supportId);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign ticket");
                return ApiErrors.Result(ex, _logger, "AssignTicket");
            }
        }

        [Authorize(Roles = "Support")]
        [HttpPut("{ticketId}")]
        public async Task<IActionResult> UpdateTicket(int ticketId, [FromBody] UpdateSupportTicketDto updateDto)
        {
            try
            {
                var ticket = await _ticketService.UpdateTicketAsync(ticketId, updateDto);
                _logger.LogInformation("Ticket updated: {TicketId}", ticketId);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ticket");
                return ApiErrors.Result(ex, _logger, "UpdateTicket");
            }
        }
    }
}
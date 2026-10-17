using TruthEye.DTOs;

namespace TruthEye.Services
{
    public interface ISupportTicketService
    {
        Task<SupportTicketDto> CreateTicketAsync(int userId, CreateSupportTicketDto ticketDto);
        Task<SupportTicketDto> AssignTicketAsync(int ticketId, int supportId);
        Task<SupportTicketDto> UpdateTicketAsync(int ticketId, UpdateSupportTicketDto updateDto);
        Task<IEnumerable<SupportTicketDto>> GetUserTicketsAsync(int userId);
        Task<IEnumerable<SupportTicketDto>> GetAssignedTicketsAsync(int supportId);
        Task<IEnumerable<SupportTicketDto>> GetAllTicketsAsync(string status = null);
    }
}

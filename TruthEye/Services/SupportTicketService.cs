using AutoMapper;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class SupportTicketService : ISupportTicketService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SupportTicketService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SupportTicketDto> CreateTicketAsync(int userId, CreateSupportTicketDto ticketDto)
        {
            var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                UserId = userId,
                Category = ticketDto.Category,
                Priority = ticketDto.Priority,
                Subject = ticketDto.Subject,
                Description = ticketDto.Description,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<SupportTicket>().AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportTicketDto>(ticket);
        }

        public async Task<SupportTicketDto> AssignTicketAsync(int ticketId, int supportId)
        {
            var ticket = await _unitOfWork.Repository<SupportTicket>().GetByIdAsync(ticketId);
            if (ticket == null)
                throw new Exception("Ticket not found");

            var support = await _unitOfWork.Repository<Support>().GetByIdAsync(supportId);
            if (support == null)
                throw new Exception("Support user not found");

            ticket.AssignedToSupportId = supportId;
            ticket.Status = "In Progress";

            support.OpenTickets++;

            await _unitOfWork.Repository<SupportTicket>().UpdateAsync(ticket);
            await _unitOfWork.Repository<Support>().UpdateAsync(support);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportTicketDto>(ticket);
        }

        public async Task<SupportTicketDto> UpdateTicketAsync(int ticketId, UpdateSupportTicketDto updateDto)
        {
            var ticket = await _unitOfWork.Repository<SupportTicket>().GetByIdAsync(ticketId);
            if (ticket == null)
                throw new Exception("Ticket not found");

            ticket.Status = updateDto.Status;
            ticket.Resolution = updateDto.Resolution;

            if (updateDto.Status == "Resolved" || updateDto.Status == "Closed")
            {
                ticket.ResolvedAt = DateTime.UtcNow;

                if (ticket.AssignedToSupportId.HasValue)
                {
                    var support = await _unitOfWork.Repository<Support>().GetByIdAsync(ticket.AssignedToSupportId.Value);
                    if (support != null)
                    {
                        support.OpenTickets--;
                        support.TicketsHandled++;
                        support.LastTicketHandled = DateTime.UtcNow;

                        var resolutionTime = (ticket.ResolvedAt.Value - ticket.CreatedAt).TotalHours;
                        support.AverageResolutionTime = (support.AverageResolutionTime * (support.TicketsHandled - 1) + resolutionTime) / support.TicketsHandled;

                        await _unitOfWork.Repository<Support>().UpdateAsync(support);
                    }
                }
            }

            await _unitOfWork.Repository<SupportTicket>().UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportTicketDto>(ticket);
        }

        public async Task<IEnumerable<SupportTicketDto>> GetUserTicketsAsync(int userId)
        {
            var tickets = await _unitOfWork.Repository<SupportTicket>()
                .FindAsync(t => t.UserId == userId);
            return _mapper.Map<IEnumerable<SupportTicketDto>>(tickets);
        }

        public async Task<IEnumerable<SupportTicketDto>> GetAssignedTicketsAsync(int supportId)
        {
            var tickets = await _unitOfWork.Repository<SupportTicket>()
                .FindAsync(t => t.AssignedToSupportId == supportId);
            return _mapper.Map<IEnumerable<SupportTicketDto>>(tickets);
        }

        public async Task<IEnumerable<SupportTicketDto>> GetAllTicketsAsync(string status = null)
        {
            var tickets = string.IsNullOrEmpty(status)
                ? await _unitOfWork.Repository<SupportTicket>().GetAllAsync()
                : await _unitOfWork.Repository<SupportTicket>().FindAsync(t => t.Status == status);

            return _mapper.Map<IEnumerable<SupportTicketDto>>(tickets);
        }
    }
}

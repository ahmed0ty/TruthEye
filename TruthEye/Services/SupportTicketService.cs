using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Exceptions;
using TruthEye.UnitOfWorkk;
using TruthEye.Entities.Data;
namespace TruthEye.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private static readonly string[] Statuses = { "Open", "In Progress", "Resolved", "Closed" };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SupportTicketService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SupportTicketDto> CreateTicketAsync(int userId, CreateSupportTicketDto ticketDto)
        {
            var subject = ticketDto.Subject?.Trim() ?? "";
            var description = ticketDto.Description?.Trim() ?? "";
            if (subject.Length < 3 || subject.Length > 200) throw new BadRequestException("Subject must be 3 to 200 characters");
            if (description.Length < 5 || description.Length > 4000) throw new BadRequestException("Description must be 5 to 4000 characters");

            var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                UserId = userId,
                Category = ticketDto.Category,
                Priority = ticketDto.Priority,
                Subject = subject,
                Description = description,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<SupportTicket>().AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportTicketDto>(ticket);
        }

        public async Task<SupportTicketDto> AssignTicketAsync(int ticketId, int supportId)
        {
            var ticket = await _unitOfWork.Repository<SupportTicket>().GetByIdAsync(ticketId)
                ?? throw new NotFoundException("Ticket not found");
            if (ticket.Status is "Resolved" or "Closed")
                throw new BadRequestException("The ticket is already closed");

            var support = await _unitOfWork.Repository<Support>().GetByIdAsync(supportId);
            if (support == null || !support.IsActive)
                throw new NotFoundException("Support user not found");

            if (ticket.AssignedToSupportId != supportId)
            {
                // نخصم من المعيَّن القديم قبل ما نزوّد للجديد
                if (ticket.AssignedToSupportId is int prevId)
                {
                    var prev = await _unitOfWork.Repository<Support>().GetByIdAsync(prevId);
                    if (prev != null && prev.OpenTickets > 0)
                    {
                        prev.OpenTickets--;
                        await _unitOfWork.Repository<Support>().UpdateAsync(prev);
                    }
                }
                support.OpenTickets++;
                await _unitOfWork.Repository<Support>().UpdateAsync(support);
            }

            ticket.AssignedToSupportId = supportId;
            ticket.Status = "In Progress";

            await _unitOfWork.Repository<SupportTicket>().UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupportTicketDto>(ticket);
        }

        public async Task<SupportTicketDto> UpdateTicketAsync(int ticketId, UpdateSupportTicketDto updateDto)
        {
            var ticket = await _unitOfWork.Repository<SupportTicket>().GetByIdAsync(ticketId)
                ?? throw new NotFoundException("Ticket not found");

            if (!Statuses.Contains(updateDto.Status))
                throw new BadRequestException("Invalid status");
            if (updateDto.Resolution?.Length > 2000)
                throw new BadRequestException("The resolution is too long");

            var wasClosed = ticket.Status is "Resolved" or "Closed";
            var isClosed = updateDto.Status is "Resolved" or "Closed";

            ticket.Status = updateDto.Status;
            ticket.Resolution = updateDto.Resolution;

            if (isClosed && !wasClosed)
            {
                ticket.ResolvedAt = DateTime.UtcNow;

                if (ticket.AssignedToSupportId.HasValue)
                {
                    var support = await _unitOfWork.Repository<Support>().GetByIdAsync(ticket.AssignedToSupportId.Value);
                    if (support != null)
                    {
                        if (support.OpenTickets > 0) support.OpenTickets--;
                        support.TicketsHandled++;
                        support.LastTicketHandled = DateTime.UtcNow;

                        var resolutionTime = (ticket.ResolvedAt.Value - ticket.CreatedAt).TotalHours;
                        support.AverageResolutionTime = (support.AverageResolutionTime * (support.TicketsHandled - 1) + resolutionTime) / support.TicketsHandled;

                        await _unitOfWork.Repository<Support>().UpdateAsync(support);
                    }
                }
            }
            else if (!isClosed)
            {
                ticket.ResolvedAt = null;
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

        public async Task<IEnumerable<SupportTicketDto>> GetAllTicketsAsync(string? status = null, int? facultyId = null)
        {
            List<int>? userIds = null;

            if (facultyId is int f)
            {
                // مستخدمي الكلية: طلاب ودكاترة ومراقبين وإداريين
                userIds = new List<int>();
                userIds.AddRange(await _unitOfWork.Repository<Student>().GetQueryable().Where(x => x.FacultyId == f).Select(x => x.Id).ToListAsync());
                userIds.AddRange(await _unitOfWork.Repository<Doctor>().GetQueryable().Where(x => x.FacultyId == f).Select(x => x.Id).ToListAsync());
                userIds.AddRange(await _unitOfWork.Repository<Proctor>().GetQueryable().Where(x => x.FacultyId == f).Select(x => x.Id).ToListAsync());
                userIds.AddRange(await _unitOfWork.Repository<Admin>().GetQueryable().Where(x => x.FacultyId == f).Select(x => x.Id).ToListAsync());
            }

            var tickets = await _unitOfWork.Repository<SupportTicket>().FindAsync(
                t => (string.IsNullOrEmpty(status) || t.Status == status)
                     && (userIds == null || userIds.Contains(t.UserId)),
                t => t.User, t => t.AssignedToSupport);

            return _mapper.Map<IEnumerable<SupportTicketDto>>(tickets.OrderByDescending(t => t.CreatedAt));
        }
    }
}
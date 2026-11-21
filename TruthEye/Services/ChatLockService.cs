using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TruthEye.Data;

namespace TruthEye.Services
{
    public sealed record ChatLockInfo(bool Locked, DateTime? Until);

    public interface IChatLockService
    {
        Task<ChatLockInfo> GetAsync(int facultyId);
    }

    public class ChatLockService : IChatLockService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public ChatLockService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<ChatLockInfo> GetAsync(int facultyId)
        {
            if (facultyId <= 0) return new ChatLockInfo(false, null);

            return (await _cache.GetOrCreateAsync($"chatlock:{facultyId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
                var now = DateTime.UtcNow;

                var until = await _db.Exams.AsNoTracking()
                    .Where(e => !e.IsDeleted
                                && e.Status == "Active"
                                && e.StartTime <= now && e.EndTime > now
                                && e.Course.Department.FacultyId == facultyId)
                    .MaxAsync(e => (DateTime?)e.EndTime);

                return new ChatLockInfo(until != null, until);
            }))!;
        }
    }
}
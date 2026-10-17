using System.ComponentModel.DataAnnotations;
using TruthEye.Interfaces;

namespace TruthEye.Entities
{
    public class BaseEntity: ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

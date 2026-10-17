namespace TruthEye.Entities.Data
{
    public class Admin: User
    {
        public int UniversityId { get; set; }    
        public virtual University University { get; set; }
        public int FacultyId { get; set; }
        public virtual Faculty Faculty { get; set; }

    }
}

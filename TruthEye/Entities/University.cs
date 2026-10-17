namespace TruthEye.Entities
{
    public class University
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<Faculty> Faculties { get; set; }
    }
}

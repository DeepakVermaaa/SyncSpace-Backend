using WebAPI.Models;

namespace SyncSpaceBackend.Models
{
    public class Organizations
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Domain { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<ProjectGroup> Projects { get; set; }
    }
}

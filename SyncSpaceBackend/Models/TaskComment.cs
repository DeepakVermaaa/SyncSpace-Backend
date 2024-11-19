using WebAPI.Models;

namespace SyncSpaceBackend.Models
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }

        // Navigation properties
        public virtual ProjectTask Task { get; set; }
        public virtual User CreatedBy { get; set; }
    }
}

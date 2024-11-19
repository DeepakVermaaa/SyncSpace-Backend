using WebAPI.Models;
using static SyncSpaceBackend.Enums.Enum;
using TaskStatus = SyncSpaceBackend.Enums.Enum.TaskStatus;

namespace SyncSpaceBackend.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AssignedToId { get; set; }
        public int CreatedById { get; set; }

        // Navigation properties
        public virtual ProjectGroup Project { get; set; }
        public virtual User AssignedTo { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<TaskComment> Comments { get; set; }
        public virtual ICollection<TaskAttachment> Attachments { get; set; }
    }
}

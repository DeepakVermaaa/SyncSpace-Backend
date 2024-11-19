using WebAPI.Models;
using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Models
{
    public class ProjectMilestone
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }

        // Navigation properties
        public virtual ProjectGroup Project { get; set; }
        public virtual User CreatedBy { get; set; }
    }
}

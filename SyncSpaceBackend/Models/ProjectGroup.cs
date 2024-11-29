using WebAPI.Models;
using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Models
{
    public class ProjectGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public int OrganizationId { get; set; }
        public DateTime? EndDate { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        // Navigation properties
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        public virtual ICollection<ChatRoom> ChatRooms { get; set; }
        public virtual ICollection<ChatMessage> ChatMessages { get; set; }
        public virtual ICollection<ProjectTask> Tasks { get; set; }
        public virtual Organizations Organizations { get; set; }
        public virtual ICollection<ProjectMilestone> Milestones { get; set; }
    }

}

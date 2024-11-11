using WebAPI.Models;

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

        // Navigation properties
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        public virtual ICollection<ChatRoom> ChatRooms { get; set; }
        public virtual ICollection<ChatMessage> ChatMessages { get; set; }
    }

}

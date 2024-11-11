using SyncSpaceBackend.Enums;
using WebAPI.Models;

namespace SyncSpaceBackend.Models
{
    public class ProjectMember
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string UserId { get; set; }
        public ProjectRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual ProjectGroup Project { get; set; }
        public virtual User User { get; set; }
    }
}

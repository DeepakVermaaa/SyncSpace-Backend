using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.DTO
{
    public class ProjectMemberAddDto
    {
        public int UserId { get; set; }
        public ProjectRole Role { get; set; }
    }
}

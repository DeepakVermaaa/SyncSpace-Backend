using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.DTO
{
    public class ProjectMemberResponseDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ProjectRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}

using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Models
{
    public class UpdatePermissionRequest
    {
        public int UserId { get; set; }
        public DocumentPermissionLevel PermissionLevel { get; set; }
    }
}

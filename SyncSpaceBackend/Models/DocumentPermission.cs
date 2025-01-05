using static SyncSpaceBackend.Enums.Enum;

namespace WebAPI.Models
{
    public class DocumentPermission
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public DocumentPermissionLevel PermissionLevel { get; set; }

        public virtual Document Document { get; set; }
        public virtual User User { get; set; }
    }
}
using WebAPI.Models;

namespace SyncSpaceBackend.Models
{
    public class TaskAttachment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedById { get; set; }

        // Navigation properties
        public virtual ProjectTask Task { get; set; }
        public virtual User UploadedBy { get; set; }
    }
}

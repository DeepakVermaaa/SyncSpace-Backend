using System;

namespace WebAPI.Models
{
    public class DocumentVersion
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedById { get; set; }
        public string? Comment { get; set; }
        public virtual Document Document { get; set; }
        public virtual User UploadedBy { get; set; }
    }
}
using SyncSpaceBackend.Models;
using System;

namespace WebAPI.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedById { get; set; }
        public int ProjectGroupId { get; set; }
        public int CurrentVersionId { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User UploadedBy { get; set; }
        public virtual ProjectGroup Project { get; set; }
        public virtual ICollection<DocumentVersion> Versions { get; set; }
        public virtual ICollection<DocumentPermission> Permissions { get; set; }
    }
}
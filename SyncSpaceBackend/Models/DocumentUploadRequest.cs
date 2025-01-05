namespace SyncSpaceBackend.Models
{
    public class DocumentUploadRequest
    {
        public IFormFile File { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public int ProjectId { get; set; }
    }
}

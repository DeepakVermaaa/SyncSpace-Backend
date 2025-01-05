namespace SyncSpaceBackend.Models
{
    public class DocumentUpdateRequest
    {
        public IFormFile File { get; set; }
        public string Comment { get; set; }
    }
}

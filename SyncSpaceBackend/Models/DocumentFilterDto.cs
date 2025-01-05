namespace SyncSpaceBackend.Models
{
    public class DocumentFilterDto
    {
        public string? SearchQuery { get; set; }
        public string? FileType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

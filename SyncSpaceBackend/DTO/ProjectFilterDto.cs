using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.DTO
{
    public class ProjectFilterDto
    {
        public string? SearchQuery { get; set; }
        public ProjectStatus? Status { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

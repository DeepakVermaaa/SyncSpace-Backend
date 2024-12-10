using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.DTO
{
    public class TaskFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchQuery { get; set; }
        public TaskStatusEnum? Status { get; set; }
        public TaskPriorityEnum? Priority { get; set; }
        public int? ProjectId { get; set; }
        public bool AssignedToMe { get; set; }
        public bool CreatedByMe { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public int? SprintId { get; set; }
        public bool ShowBacklogOnly { get; set; }
    }

}

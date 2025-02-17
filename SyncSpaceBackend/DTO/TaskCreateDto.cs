﻿using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.DTO
{
    public class TaskCreateDto
    {
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string? Description { get; set; }
        public TaskPriorityEnum Priority { get; set; }
        public TaskStatusEnum Status { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToId { get; set; }
    }
}

namespace SyncSpaceBackend.DTO
{
    public class TaskActivityLogDto
    {
        public int TaskId { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Changes { get; set; }
    }
}

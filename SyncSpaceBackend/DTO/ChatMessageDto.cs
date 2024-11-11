namespace SyncSpaceBackend.DTO
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public int ProjectGroupId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

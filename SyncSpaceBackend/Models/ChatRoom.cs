namespace SyncSpaceBackend.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }
        public int ProjectGroupId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ProjectGroup ProjectGroup { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; }
    }
}

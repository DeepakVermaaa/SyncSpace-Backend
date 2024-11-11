using WebAPI.Models;

namespace SyncSpaceBackend.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public int ProjectGroupId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public virtual ProjectGroup ProjectGroup { get; set; }
        public virtual User Sender { get; set; }
    }
}

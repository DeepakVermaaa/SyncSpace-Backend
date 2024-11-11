namespace SyncSpaceBackend.DTO
{
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public int ProjectGroupId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public ProjectGroupDto ProjectGroup { get; set; }
    }
}

using WebAPI.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; }
    public NotificationType? Type { get; set; }
    public string? ReferenceId { get; set; }  // ID of related item (task, project, etc.)
    public bool IsRead { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation property
    public virtual User? User { get; set; }
}
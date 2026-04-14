namespace TaskManagement.API.Models;

public class ApprovalLog
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ReviewedBy { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserTask Task { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}

namespace TaskManagement.API.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

public class TaskResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

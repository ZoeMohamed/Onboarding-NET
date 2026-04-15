using System.ComponentModel.DataAnnotations;

namespace TaskManagement.API.DTOs;

public class CreateTaskDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskDto
{
    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

public class ReviewTaskDto
{
    [MaxLength(500)]
    public string? Note { get; set; }
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

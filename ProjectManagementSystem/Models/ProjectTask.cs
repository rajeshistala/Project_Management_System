namespace ProjectManagementSystem.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.ToDo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsDeleted { get; set; } = false; // soft delete

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        // Nullable: a task can be unassigned
        public string? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; }

        // Self-referencing: simple one-level dependency (this task blocks on another)
        public int? DependsOnTaskId { get; set; }
        public ProjectTask? DependsOnTask { get; set; }

        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
    }

    public class TaskComment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TaskId { get; set; }
        public ProjectTask? Task { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser? Author { get; set; }
    }

    // Lightweight audit trail entry, e.g. "Status changed from ToDo to InProgress"
    public class TaskActivityLog
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int TaskId { get; set; }
        public ProjectTask? Task { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}

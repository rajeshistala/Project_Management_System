namespace ProjectManagementSystem.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public bool IsArchived { get; set; } = false;

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser? Owner { get; set; }

        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }

    // Join table: which users belong to which projects, and their role within that project
    public class ProjectMember
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public ProjectRole Role { get; set; } = ProjectRole.Employee;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public bool IsRemoved { get; set; } = false;
        public DateTime? RemovedAt { get; set; }
    }
}

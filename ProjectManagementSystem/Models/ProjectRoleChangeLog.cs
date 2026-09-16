namespace ProjectManagementSystem.Models
{
    public enum RoleChangeType
    {
        Removed = 0,      // taken off the project, nobody put in their place
        Replaced = 1,     // taken off and someone else put in their place
        RoleChanged = 2   // stayed on the project, but their role changed
    }
    public class ProjectRoleChangeLog
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public string AffectedUserId { get; set; } = string.Empty;
        public ApplicationUser? AffectedUser { get; set; }

        public string ChangedByUserId { get; set; } = string.Empty;
        public ApplicationUser? ChangedByUser { get; set; }

        public string? ReplacementUserId { get; set; }
        public ApplicationUser? ReplacementUser { get; set; }

        public RoleChangeType ChangeType { get; set; }
        public ProjectRole OldRole { get; set; }
        public ProjectRole? NewRole { get; set; }

        public string? Reason { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public int TasksReassigned { get; set; }
    }
}

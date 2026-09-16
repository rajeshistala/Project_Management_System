using ProjectManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem.DTOs
{
    public class RemoveMemberDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string? ReplacementUserId { get; set; }
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class RoleChangeLogDto
    {
        public int Id { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string AffectedUserId { get; set; } = string.Empty;
        public string? AffectedUserName { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;
        public string? ChangedByUserName { get; set; }
        public string? ReplacementUserId { get; set; }
        public string? ReplacementUserName { get; set; }
        public ProjectRole OldRole { get; set; }
        public string? Reason { get; set; }
        public int TasksReassigned { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class TaskHandoverDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ProjectTaskStatus Status { get; set; }
        public int ProgressPercent { get; set; }
        public string? PreviousAssigneeName { get; set; }
        public List<CommentResponseDto> Comments { get; set; } = new();
        public List<string> ActivityHistory { get; set; } = new();
    }
}

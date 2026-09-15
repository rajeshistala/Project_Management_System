using ProjectManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem.DTOs
{
    public class CreateProjectDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateProjectDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsArchived { get; set; }
    }

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsArchived { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public int TaskCount { get; set; }
        public int MemberCount { get; set; }
    }

    public class AddProjectMemberDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ProjectRole Role { get; set; } = ProjectRole.Employee;
    }
}

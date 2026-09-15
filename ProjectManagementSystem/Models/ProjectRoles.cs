using FluentFTP.Helpers;

namespace ProjectManagementSystem.Models
{

    // A user's role WITHIN a specific project (see ProjectMember)
    public enum ProjectRole
    {
        Manager = 0,
        TeamLeader = 1,
        Employee = 2
    }

    // A user's role at the COMPANY level (see SystemRoles below) — separate from
    // project role. HR is system-wide and ignores project membership entirely.
    public static class SystemRoles
    {
        public const string HR = "HR";
        public const string Manager = "Manager";
        public const string TeamLeader = "TeamLeader";
        public const string Employee = "Employee";

        public static readonly string[] All = { HR, Manager, TeamLeader, Employee };
    }

    public enum ProjectTaskStatus
    {
        ToDo = 0,
        InProgress = 1,
        InReview = 2,
        Done = 3
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }
}

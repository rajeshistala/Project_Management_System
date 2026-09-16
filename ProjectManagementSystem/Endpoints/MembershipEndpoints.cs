using ProjectManagementSystem.DTOs;
using ProjectManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ProjectManagementSystem.Endpoints
{
    public static class MembershipEndpoints
    {
        public static void MapMembershipEndpoints(this WebApplication app)
        {
            // MapGroup shares the route prefix, Swagger tag, and auth requirement
            // across every endpoint below — the Minimal API equivalent of putting
            // [Route(...)] and [Authorize] on a controller class.
            var group = app.MapGroup("/api/projects/{projectId:int}/membership")
                .WithTags("Membership")
                .RequireAuthorization();

            group.MapPost("/remove", RemoveMember)
                .WithSummary("Remove or replace a project member, handing over their open tasks.");

            group.MapGet("/history", GetChangeHistory)
                .WithSummary("Full audit trail of removals, replacements, and role changes.");

            group.MapGet("/handover/{taskId:int}", GetTaskHandover)
                .WithSummary("Everything the new assignee needs: prior work, comments, and history.");
        }

        // In Minimal APIs, dependencies arrive as method parameters instead of
        // through a constructor. ClaimsPrincipal gives us the logged-in user.
        private static async Task<IResult> RemoveMember(
            int projectId,
            RemoveMemberDto dto,
            ApplicationDbContext db,
            ClaimsPrincipal principal,
            ILogger<Program> logger)
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project is null) return Results.NotFound(new { message = "Project not found." });

            var actor = project.Members.FirstOrDefault(m => m.UserId == currentUserId && !m.IsRemoved);
            var target = project.Members.FirstOrDefault(m => m.UserId == dto.UserId && !m.IsRemoved);

            if (target is null)
                return Results.NotFound(new { message = "That user is not an active member of this project." });

            // --- Permission rules ---
            // Manager can remove a TeamLeader or Employee.
            // TeamLeader can remove an Employee only.
            // Nobody can remove the project owner, and nobody can remove themselves.
            var isOwner = project.OwnerId == currentUserId;
            var actorRole = actor?.Role;

            if (dto.UserId == currentUserId)
                return Results.BadRequest(new { message = "You cannot remove yourself from a project." });

            if (dto.UserId == project.OwnerId)
                return Results.BadRequest(new { message = "The project owner cannot be removed." });

            var canRemove = isOwner
                || actorRole == ProjectRole.Manager
                || (actorRole == ProjectRole.TeamLeader && target.Role == ProjectRole.Employee);

            if (!canRemove)
            {
                logger.LogWarning("Blocked removal attempt: {Actor} tried to remove {Target} from project {ProjectId}",
                    currentUserId, dto.UserId, projectId);
                return Results.Forbid();
            }

            // --- Validate the replacement, if one was given ---
            ProjectMember? replacement = null;
            if (dto.ReplacementUserId is not null)
            {
                if (dto.ReplacementUserId == dto.UserId)
                    return Results.BadRequest(new { message = "Replacement cannot be the same person being removed." });

                replacement = project.Members
                    .FirstOrDefault(m => m.UserId == dto.ReplacementUserId && !m.IsRemoved);

                if (replacement is null)
                    return Results.BadRequest(new { message = "The replacement must already be an active member of this project." });
            }

            // --- Hand over their open (not Done) tasks ---
            // Done tasks stay attributed to whoever actually finished them.
            var openTasks = await db.ProjectTasks
                .Where(t => t.ProjectId == projectId
                            && t.AssignedToId == dto.UserId
                            && t.Status != ProjectTaskStatus.Done)
                .ToListAsync();

            foreach (var task in openTasks)
            {
                task.AssignedToId = dto.ReplacementUserId; // null for a plain removal

                // The activity log is what makes the handover visible to the new
                // assignee — the task's full history stays attached to the task.
                db.TaskActivityLogs.Add(new TaskActivityLog
                {
                    TaskId = task.Id,
                    UserId = currentUserId,
                    Description = dto.ReplacementUserId is null
                        ? "Previous assignee removed from project; task is now unassigned."
                        : "Task handed over from the previous assignee following their removal."
                });
            }

            // --- Soft-remove the membership (row stays for history) ---
            target.IsRemoved = true;
            target.RemovedAt = DateTime.UtcNow;

            // If a TeamLeader was replaced, the replacement inherits that role.
            if (replacement is not null && target.Role == ProjectRole.TeamLeader)
                replacement.Role = ProjectRole.TeamLeader;

            db.ProjectRoleChangeLogs.Add(new ProjectRoleChangeLog
            {
                ProjectId = projectId,
                AffectedUserId = dto.UserId,
                ChangedByUserId = currentUserId,
                ReplacementUserId = dto.ReplacementUserId,
                ChangeType = dto.ReplacementUserId is null ? RoleChangeType.Removed : RoleChangeType.Replaced,
                OldRole = target.Role,
                Reason = dto.Reason,
                TasksReassigned = openTasks.Count
            });

            await db.SaveChangesAsync();

            logger.LogInformation(
                "User {Target} ({Role}) removed from project {ProjectId} by {Actor}. {Count} task(s) handed over to {Replacement}.",
                dto.UserId, target.Role, projectId, currentUserId, openTasks.Count, dto.ReplacementUserId ?? "nobody");

            return Results.Ok(new
            {
                message = dto.ReplacementUserId is null
                    ? "Member removed. Their open tasks are now unassigned."
                    : "Member replaced. Their open tasks were handed over.",
                tasksReassigned = openTasks.Count
            });
        }

        private static async Task<IResult> GetChangeHistory(
            int projectId,
            ApplicationDbContext db,
            ClaimsPrincipal principal)
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();

            var isMember = await db.Projects.AnyAsync(p => p.Id == projectId &&
                (p.OwnerId == currentUserId || p.Members.Any(m => m.UserId == currentUserId && !m.IsRemoved)));
            if (!isMember) return Results.Forbid();

            var history = await db.ProjectRoleChangeLogs
                .Where(l => l.ProjectId == projectId)
                .OrderByDescending(l => l.ChangedAt)
                .Select(l => new RoleChangeLogDto
                {
                    Id = l.Id,
                    ChangeType = l.ChangeType.ToString(),
                    AffectedUserId = l.AffectedUserId,
                    AffectedUserName = l.AffectedUser!.FullName,
                    ChangedByUserId = l.ChangedByUserId,
                    ChangedByUserName = l.ChangedByUser!.FullName,
                    ReplacementUserId = l.ReplacementUserId,
                    ReplacementUserName = l.ReplacementUser != null ? l.ReplacementUser.FullName : null,
                    OldRole = l.OldRole,
                    Reason = l.Reason,
                    TasksReassigned = l.TasksReassigned,
                    ChangedAt = l.ChangedAt
                })
                .ToListAsync();

            return Results.Ok(history);
        }

        // Solves the core requirement: "the newly assigned person should know
        // what the previous person did and what to do next."
        private static async Task<IResult> GetTaskHandover(
            int projectId,
            int taskId,
            ApplicationDbContext db,
            ClaimsPrincipal principal)
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();

            var isMember = await db.Projects.AnyAsync(p => p.Id == projectId &&
                (p.OwnerId == currentUserId || p.Members.Any(m => m.UserId == currentUserId && !m.IsRemoved)));
            if (!isMember) return Results.Forbid();

            var task = await db.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (task is null) return Results.NotFound();

            var comments = await db.TaskComments
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author!.FullName
                })
                .ToListAsync();

            var activity = await db.TaskActivityLogs
                .Where(l => l.TaskId == taskId)
                .OrderBy(l => l.Timestamp)
                .Select(l => l.Timestamp.ToString("yyyy-MM-dd HH:mm") + " — " + l.Description)
                .ToListAsync();

            // Who held this task most recently before the current assignee.
            var previousAssigneeName = await db.ProjectRoleChangeLogs
                .Where(l => l.ProjectId == projectId && l.ReplacementUserId == task.AssignedToId)
                .OrderByDescending(l => l.ChangedAt)
                .Select(l => l.AffectedUser!.FullName)
                .FirstOrDefaultAsync();

            return Results.Ok(new TaskHandoverDto
            {
                TaskId = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                ProgressPercent = task.Status switch
                {
                    ProjectTaskStatus.Done => 100,
                    ProjectTaskStatus.InReview => 75,
                    ProjectTaskStatus.InProgress => 50,
                    _ => 0
                },
                PreviousAssigneeName = previousAssigneeName,
                Comments = comments,
                ActivityHistory = activity
            });
        }
    }
}

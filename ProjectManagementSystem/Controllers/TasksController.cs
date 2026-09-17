using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem.DTOs;
using ProjectManagementSystem.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TasksController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User id claim missing.");

        // HR ignores project membership entirely — company-wide visibility by role.
        private bool IsHr => User.IsInRole(SystemRoles.HR);

        private async Task<bool> IsProjectMember(int projectId, string userId) =>
            IsHr || await _db.Projects.AnyAsync(p => p.Id == projectId &&
                (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId && !m.IsRemoved)));

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasks(
            int projectId, [FromQuery] ProjectTaskStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.ProjectTasks.Where(t => t.ProjectId == projectId);
            if (status.HasValue) query = query.Where(t => t.Status == status.Value);

            var tasks = await query
                .OrderBy(t => t.DueDate == null)
                .ThenBy(t => t.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedAt = t.CreatedAt,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    IsOverdue = t.DueDate != null && t.DueDate < DateTime.UtcNow && t.Status != ProjectTaskStatus.Done,
                    ProjectId = t.ProjectId,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                    DependsOnTaskId = t.DependsOnTaskId,
                    IsBlocked = t.DependsOnTask != null && t.DependsOnTask.Status != ProjectTaskStatus.Done
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask(int projectId, CreateTaskDto dto)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            if (dto.DependsOnTaskId.HasValue)
            {
                var dependencyValid = await _db.ProjectTasks
                    .AnyAsync(t => t.Id == dto.DependsOnTaskId && t.ProjectId == projectId);
                if (!dependencyValid) return BadRequest(new { message = "DependsOnTaskId must belong to the same project." });
            }

            var task = new ProjectTask
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                ProjectId = projectId,
                AssignedToId = dto.AssignedToId,
                DependsOnTaskId = dto.DependsOnTaskId
            };

            _db.ProjectTasks.Add(task);
            await _db.SaveChangesAsync();

            _db.TaskActivityLogs.Add(new TaskActivityLog
            {
                TaskId = task.Id,
                UserId = userId,
                Description = "Task created."
            });
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTasks), new { projectId }, new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
                AssignedToId = task.AssignedToId
            });
        }

        [HttpPut("{taskId:int}")]
        public async Task<IActionResult> UpdateTask(int projectId, int taskId, UpdateTaskDto dto)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            var task = await _db.ProjectTasks
                .Include(t => t.DependsOnTask)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (task == null) return NotFound();

            if (dto.Status == ProjectTaskStatus.Done &&
                task.DependsOnTask != null && task.DependsOnTask.Status != ProjectTaskStatus.Done)
            {
                return BadRequest(new { message = $"Task is blocked by '{task.DependsOnTask.Title}', which isn't Done yet." });
            }

            var statusChanged = task.Status != dto.Status;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            task.AssignedToId = dto.AssignedToId;
            task.DependsOnTaskId = dto.DependsOnTaskId;
            task.CompletedAt = dto.Status == ProjectTaskStatus.Done ? DateTime.UtcNow : null;

            if (statusChanged)
            {
                _db.TaskActivityLogs.Add(new TaskActivityLog
                {
                    TaskId = task.Id,
                    UserId = userId,
                    Description = $"Status changed to {dto.Status}."
                });
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{taskId:int}")]
        public async Task<IActionResult> DeleteTask(int projectId, int taskId)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            var task = await _db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (task == null) return NotFound();

            task.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{taskId:int}/comments")]
        public async Task<ActionResult<CommentResponseDto>> AddComment(int projectId, int taskId, CreateCommentDto dto)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            var taskExists = await _db.ProjectTasks.AnyAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (!taskExists) return NotFound();

            var comment = new TaskComment
            {
                TaskId = taskId,
                AuthorId = userId,
                Content = dto.Content
            };

            _db.TaskComments.Add(comment);
            await _db.SaveChangesAsync();

            return Ok(new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorId = userId
            });
        }

        [HttpGet("{taskId:int}/comments")]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetComments(int projectId, int taskId)
        {
            var userId = CurrentUserId;
            if (!await IsProjectMember(projectId, userId)) return Forbid();

            var comments = await _db.TaskComments
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

            return Ok(comments);
        }
    }
}
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
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProjectsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User id claim missing.");
        // HR ignores project membership entirely — company-wide visibility by role,
        // not by being added to each project individually.
        private bool IsHr => User.IsInRole(SystemRoles.HR);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetProjects(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var userId = CurrentUserId;
            var isHr = IsHr;

            var projects = await _db.Projects
                .Where(p => isHr || p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    DueDate = p.DueDate,
                    IsArchived = p.IsArchived,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner!.FullName,
                    TaskCount = p.Tasks.Count(t => !t.IsDeleted),
                    MemberCount = p.Members.Count
                })
                .ToListAsync();

            return Ok(projects);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectResponseDto>> GetProject(int id)
        {
            var userId = CurrentUserId;
            var isHr = IsHr;
            var project = await _db.Projects
                .Where(p => p.Id == id && (isHr || p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)))
                .Select(p => new ProjectResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    DueDate = p.DueDate,
                    IsArchived = p.IsArchived,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner!.FullName,
                    TaskCount = p.Tasks.Count(t => !t.IsDeleted),
                    MemberCount = p.Members.Count
                })
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();
            return Ok(project);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> CreateProject(CreateProjectDto dto)
        {
            var userId = CurrentUserId;

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                DueDate = dto.DueDate,
                OwnerId = userId
            };

            _db.Projects.Add(project);

            _db.ProjectMembers.Add(new ProjectMember
            {
                Project = project,
                UserId = userId,
                Role = ProjectRole.Manager
            });

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                DueDate = project.DueDate,
                IsArchived = project.IsArchived,
                OwnerId = project.OwnerId,
                TaskCount = 0,
                MemberCount = 1
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDto dto)
        {
            var userId = CurrentUserId;

            var project = await _db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var isOwner = project.OwnerId == userId;
            var isManager = project.Members.Any(m => m.UserId == userId && m.Role == ProjectRole.Manager);
            if (!isOwner && !isManager && !IsHr) return Forbid();

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.DueDate = dto.DueDate;
            project.IsArchived = dto.IsArchived;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = CurrentUserId;
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            // Deletion stays owner-only even for HR — HR gets full VISIBILITY
            // and oversight, but permanently destroying a project is a
            // narrower power reserved for whoever actually owns it.
            if (project.OwnerId != userId) return Forbid();

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMember(int id, AddProjectMemberDto dto)
        {
            var userId = CurrentUserId;
            var project = await _db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            var isOwner = project.OwnerId == userId;
            var isManager = project.Members.Any(m => m.UserId == userId && m.Role == ProjectRole.Manager);
            if (!isOwner && !isManager && !IsHr) return Forbid();

            if (project.Members.Any(m => m.UserId == dto.UserId))
                return Conflict(new { message = "User is already a member of this project." });

            _db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = id,
                UserId = dto.UserId,
                Role = dto.Role
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
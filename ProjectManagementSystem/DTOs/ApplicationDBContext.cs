using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ProjectManagementSystem.DTOs
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();
        public DbSet<ProjectRoleChangeLog> ProjectRoleChangeLogs => Set<ProjectRoleChangeLog>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectMember>()
                .HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.DependsOnTask)
                .WithMany()
                .HasForeignKey(t => t.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTask>().HasQueryFilter(t => !t.IsDeleted);

            builder.Entity<TaskComment>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskComment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskActivityLog>()
                .HasOne(l => l.Task)
                .WithMany(t => t.ActivityLogs)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectRoleChangeLog>()
                .HasOne(l => l.Project).WithMany()
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectRoleChangeLog>()
                .HasOne(l => l.AffectedUser).WithMany()
                .HasForeignKey(l => l.AffectedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectRoleChangeLog>()
                .HasOne(l => l.ChangedByUser).WithMany()
                .HasForeignKey(l => l.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectRoleChangeLog>()
                .HasOne(l => l.ReplacementUser).WithMany()
                .HasForeignKey(l => l.ReplacementUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

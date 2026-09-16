using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipRemovalAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "ProjectMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAt",
                table: "ProjectMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectRoleChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    AffectedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReplacementUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    OldRole = table.Column<int>(type: "int", nullable: false),
                    NewRole = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TasksReassigned = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRoleChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRoleChangeLogs_AspNetUsers_AffectedUserId",
                        column: x => x.AffectedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRoleChangeLogs_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRoleChangeLogs_AspNetUsers_ReplacementUserId",
                        column: x => x.ReplacementUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRoleChangeLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoleChangeLogs_AffectedUserId",
                table: "ProjectRoleChangeLogs",
                column: "AffectedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoleChangeLogs_ChangedByUserId",
                table: "ProjectRoleChangeLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoleChangeLogs_ProjectId",
                table: "ProjectRoleChangeLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoleChangeLogs_ReplacementUserId",
                table: "ProjectRoleChangeLogs",
                column: "ReplacementUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectRoleChangeLogs");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "ProjectMembers");
        }
    }
}

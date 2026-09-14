Welcome to the Project : Project Management System

## Why I Built This Project

1. I wanted to build something that solves a real, practical problem faced by
   software companies — not just a generic CRUD demo.
2. I chose a Project Management System because it touches almost every core
   backend concept: authentication, role-based access control, relational
   data modeling, business logic, and audit history — all in one project.
3. It gave me the chance to design a system around a real organizational
   hierarchy (HR → Manager → Team Leader → Employee) instead of a flat,
   oversimplified user model.
4. It let me practice proper SDLC — requirement gathering, ER design, and
   sprint-based (Agile/Scrum) development — instead of jumping straight to code.

## Problem This Project Solves

1. In a software company with multiple ongoing projects and many employees,
   Managers often struggle to track:
   - which employees are working on which projects
   - how much progress each project has actually made
2. Team Leaders assign daily tasks to employees, but have no reliable way to
   confirm whether the work was actually completed without manually asking
   each person.
3. There was no single, transparent source of truth showing task status,
   project completion percentage, and who is responsible for what.
4. When an employee underperforms, there was no structured way to reassign
   their pending work to someone else without losing the history of what had
   already been done.
5. This project solves these problems by providing:
   - a role-based system where Managers and Team Leaders can see task and
     project progress at a glance (auto-calculated completion %)
   - a task assignment and status-tracking workflow, so no one has to
     manually chase updates
   - a reassignment/removal workflow that preserves task history, so a new
     assignee can pick up exactly where the previous person left off
   - company-wide visibility for HR, without needing to be added to every
     project individually
   - email notifications, so key events (assignment, removal, replacement)
     reach the right people automatically

## Who Can Use This Project

1. **HR** — gets full visibility into every project, every employee, and
   every Team Leader/Manager across the company.
2. **Managers** — can create projects, monitor overall project progress (%),
   and replace underperforming Team Leaders or Employees.
3. **Team Leaders** — can assign daily tasks to employees, track individual
   task progress, and reassign/remove employees from tasks when needed.
4. **Employees** — can view tasks assigned to them and update their status
   as they complete work.
5. More broadly, this project is a realistic reference implementation for
   **any small-to-medium software company** that wants a lightweight,
   in-house alternative to tools like Jira or Asana, tailored to their own
   reporting hierarchy.

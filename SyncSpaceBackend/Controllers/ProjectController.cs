using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static SyncSpaceBackend.Enums.Enum;
using SyncSpaceBackend.DTO;
using SyncSpaceBackend.Models;
using System.Security.Claims;
using WebAPI.Context;
using TaskStatus = SyncSpaceBackend.Enums.Enum.TaskStatus;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace SyncSpaceBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(
            AppDbContext context,
            IWebHostEnvironment environment,
            IHubContext<NotificationHub> notificationHub,
            ILogger<ProjectController> logger)
        {
            _context = context;
            _notificationHub = notificationHub;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectFilterDto filterParams)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            // Start with base query including user access check
            var query = _context.ProjectGroups
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && p.IsActive);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.ProjectMembers.Any(m =>
                        (m.User.FirstName + " " + m.User.LastName).ToLower().Contains(searchTerm)));
            }

            // Apply status filter
            if (filterParams.Status.HasValue)
            {
                query = query.Where(p => p.Status == filterParams.Status.Value);
            }

            // Apply sorting
            query = ApplySorting(query, filterParams.SortBy, filterParams.SortDirection);

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var projects = await query
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.CreatedAt,
                    CreatedBy = new { p.CreatedBy.FirstName, p.CreatedBy.LastName },
                    Members = p.ProjectMembers.Select(pm => new
                    {
                        UserId = pm.UserId,
                        pm.User.FirstName,
                        pm.User.LastName,
                        pm.Role,
                        pm.JoinedAt
                    }),
                    TaskStats = new
                    {
                        Total = p.Tasks.Count,
                        Completed = p.Tasks.Count(t => t.Status == TaskStatus.Completed),
                        InProgress = p.Tasks.Count(t => t.Status == TaskStatus.InProgress),
                        Overdue = p.Tasks.Count(t => t.DueDate < DateTime.UtcNow && t.Status != TaskStatus.Completed)
                    },
                    Progress = p.Tasks.Any()
                        ? (double)p.Tasks.Count(t => t.Status == TaskStatus.Completed) / p.Tasks.Count * 100
                        : 0,
                    p.Status,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                Data = projects,
                TotalCount = totalCount,
                PageSize = filterParams.PageSize,
                PageNumber = filterParams.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            var project = await _context.ProjectGroups
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .Include(p => p.Milestones)
                .Include(p => p.ChatRooms)
                    .ThenInclude(cr => cr.Messages)
                .FirstOrDefaultAsync(p => p.Id == id &&
                    p.ProjectMembers.Any(pm => pm.UserId == userId));

            if (project == null)
                return NotFound(new { Message = "Project not found" });

            var projectDetails = new
            {
                project.Id,
                project.Name,
                project.Description,
                project.Status,
                project.CreatedAt,
                project.StartDate,
                project.EndDate,
                CreatedBy = new { project.CreatedBy.FirstName, project.CreatedBy.LastName },
                Members = project.ProjectMembers.Select(pm => new
                {
                    UserId = pm.UserId,
                    pm.User.FirstName,
                    pm.User.LastName,
                    pm.Role,
                    pm.JoinedAt,
                    ProfilePicture = pm.User.ProfilePicture
                }),
                Tasks = project.Tasks.Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.DueDate,
                    AssignedTo = new { t.AssignedTo.FirstName, t.AssignedTo.LastName }
                }),
                Milestones = project.Milestones.Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Description,
                    m.DueDate,
                    m.Status
                }),
                ChatRooms = project.ChatRooms.Select(cr => new
                {
                    cr.Id,
                    cr.Name,
                    cr.CreatedAt,
                    MessageCount = cr.Messages.Count
                }),
                Progress = project.Tasks.Any()
                    ? (double)project.Tasks.Count(t => t.Status == TaskStatus.Completed) / project.Tasks.Count * 100
                    : 0,
                IsActive = project.IsActive
            };
            return Ok(projectDetails);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto projectDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Validate input
                if (string.IsNullOrWhiteSpace(projectDto.Name))
                    return BadRequest(new { Message = "Project name is required" });

                if (string.IsNullOrWhiteSpace(projectDto.Description))
                    return BadRequest(new { Message = "Project description is required" });

                // Validate dates if provided
                if (projectDto.StartDate.HasValue && projectDto.EndDate.HasValue)
                {
                    if (projectDto.StartDate > projectDto.EndDate)
                        return BadRequest(new { Message = "End date must be after start date" });
                }

                var project = new ProjectGroup
                {
                    Name = projectDto.Name.Trim(),
                    Description = projectDto.Description.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    IsActive = true,
                    Status = projectDto.Status,
                    StartDate = projectDto.StartDate,
                    EndDate = projectDto.EndDate
                };

                _context.ProjectGroups.Add(project);
                await _context.SaveChangesAsync();

                // Add creator as project member with Admin role
                var projectMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = userId,
                    Role = ProjectRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };

                _context.ProjectMembers.Add(projectMember);

                // Create default project notification
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"Project '{project.Name}' has been created successfully",
                    Type = NotificationType.ProjectUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification
                await _notificationHub.Clients.Group(userId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return Ok(new { Message = "Project created successfully", ProjectId = project.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(500, new { Message = "An error occurred while creating the project" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] ProjectCreateDto projectDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var project = await _context.ProjectGroups
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (project == null)
                    return NotFound(new { Message = "Project not found" });

                // Check if user has admin rights
                var userMember = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null || userMember.Role != ProjectRole.Admin)
                    return new ObjectResult(new { Message = "Only project admins can update projects" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                // Update project properties
                project.Name = projectDto.Name.Trim();
                project.Description = projectDto.Description.Trim();
                project.Status = projectDto.Status;
                project.StartDate = projectDto.StartDate;
                project.EndDate = projectDto.EndDate;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Project updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating project {id}");
                return StatusCode(500, new { Message = "An error occurred while updating the project" });
            }
        }

        [HttpPost("{id}/tasks")]
        public async Task<IActionResult> AddTask(int id, [FromBody] TaskCreateDto taskDto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);


            var project = await _context.ProjectGroups
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { Message = "Project not found" });

            // Check if user has rights to create task
            var userMember = project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId);

            if (userMember == null)
                return Forbid();

            var task = new ProjectTask
            {
                ProjectId = id,
                Title = taskDto.Title,
                Description = taskDto.Description,
                Status = TaskStatus.Todo,
                Priority = taskDto.Priority,
                DueDate = taskDto.DueDate,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                AssignedToId = taskDto.AssignedToId
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Task created successfully", TaskId = task.Id });
        }

        [HttpPost("{id}/milestones")]
        public async Task<IActionResult> AddMilestone(int id, [FromBody] MilestoneCreateDto milestoneDto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            var project = await _context.ProjectGroups
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { Message = "Project not found" });

            // Check if user has admin rights
            var userMember = project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId &&
                    (pm.Role == ProjectRole.Admin || pm.Role == ProjectRole.Manager));

            if (userMember == null)
                return Forbid();

            var milestone = new ProjectMilestone
            {
                ProjectId = id,
                Title = milestoneDto.Title,
                Description = milestoneDto.Description,
                DueDate = milestoneDto.DueDate,
                Status = MilestoneStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.ProjectMilestones.Add(milestone);

            // Create notifications for all project members except the creator
            var notifications = project.ProjectMembers
                .Where(pm => pm.UserId != userId)
                .Select(member => new Notification
                {
                    UserId = member.UserId,
                    Message = $"New milestone '{milestoneDto.Title}' has been added to the project",
                    Type = NotificationType.ProjectUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                })
                .ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Send real-time notifications to all project members except the creator
            foreach (var notification in notifications)
            {
                await _notificationHub.Clients.Group(notification.UserId.ToString())
                    .SendAsync("ReceiveNotification", notification);
            }

            return Ok(new { Message = "Milestone created successfully", MilestoneId = milestone.Id });
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddProjectMember(int id, [FromBody] ProjectMemberAddDto memberDto)
        {
            try
            {
                // Get current user's ID from claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(currentUserId);

                if (!int.TryParse(currentUserId, out int parsedCurrentUserId))
                    return Unauthorized(new { Message = "User not authenticated" });

                // Get the project with its members
                var project = await _context.ProjectGroups
                    .Include(p => p.ProjectMembers)
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null)
                    return NotFound(new { Message = "Project not found" });

                // Check if current user has admin rights in the project
                var currentUserMember = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (currentUserMember == null || currentUserMember.Role != ProjectRole.Admin)
                    return new ObjectResult(new { Message = "Only project admins can add members" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                // Check if user exists
                var userToAdd = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == memberDto.UserId);

                if (userToAdd == null)
                    return NotFound(new { Message = "User to add not found" });

                // Check if user is already a member
                if (project.ProjectMembers.Any(pm => pm.UserId == memberDto.UserId))
                    return BadRequest(new { Message = "User is already a member of this project" });

                // Validate the role
                if (!Enum.IsDefined(typeof(ProjectRole), memberDto.Role))
                    return BadRequest(new { Message = "Invalid project role" });

                // Create new project member
                var newMember = new ProjectMember
                {
                    ProjectId = id,
                    UserId = memberDto.UserId,
                    Role = memberDto.Role,
                    JoinedAt = DateTime.UtcNow,
                };

                // Add to database
                _context.ProjectMembers.Add(newMember);
                await _context.SaveChangesAsync();

                // Prepare notification data
                var notificationData = new
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Role = memberDto.Role.ToString(),
                    AddedBy = $"{project.CreatedBy.FirstName} {project.CreatedBy.LastName}"
                };

                // Convert notification data to string
                var notificationDataString = System.Text.Json.JsonSerializer.Serialize(notificationData);

                // Create notification
                var notification = new Notification
                {
                    UserId = memberDto.UserId,
                    Message = $"You have been added to project '{project.Name}' as {memberDto.Role}",
                    Type = NotificationType.TeamUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification through SignalR
                await _notificationHub.Clients.Group((memberDto.UserId).ToString())
                    .SendAsync("ReceiveNotification", notification);

                // Return success response with member details
                return Ok(new
                {
                    Message = "Member added successfully",
                    Member = new
                    {
                        ProjectId = id,
                        UserId = userToAdd.Id,
                        UserName = $"{userToAdd.FirstName} {userToAdd.LastName}",
                        Role = memberDto.Role,
                        JoinedAt = newMember.JoinedAt,
                        ProfilePicture = userToAdd.ProfilePicture,
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding member to project {id}");
                return StatusCode(500, new { Message = "An error occurred while adding the member" });
            }
        }

        [HttpDelete("{projectId}/members/{userId}")]
        public async Task<IActionResult> RemoveProjectMember(int projectId, int userId)
        {
            try
            {
                var currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var project = await _context.ProjectGroups
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return NotFound(new { Message = "Project not found" });

                // Check if current user has admin rights
                var currentUserMember = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == currentUserId);

                if (currentUserMember == null || currentUserMember.Role != ProjectRole.Admin)
                    return new ObjectResult(new { Message = "Only project admins can remove members" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                // Find the member to remove
                var memberToRemove = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (memberToRemove == null)
                    return NotFound(new { Message = "Member not found in project" });

                // Prevent removing the last admin
                if (memberToRemove.Role == ProjectRole.Admin &&
                    project.ProjectMembers.Count(pm => pm.Role == ProjectRole.Admin) <= 1)
                {
                    return BadRequest(new { Message = "Cannot remove the last admin from the project" });
                }

                _context.ProjectMembers.Remove(memberToRemove);

                // Create notification for removed user
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"You have been removed from project '{project.Name}'",
                    Type = NotificationType.TeamUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification
                await _notificationHub.Clients.Group(userId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return Ok(new { Message = "Member removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing member from project {projectId}");
                return StatusCode(500, new { Message = "An error occurred while removing the member" });
            }
        }

        [HttpPut("{projectId}/members/{userId}/role")]
        public async Task<IActionResult> UpdateProjectMemberRole(int projectId, int userId, [FromBody] ProjectMemberRoleUpdateDto updateDto)
        {
            try
            {
                var currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var project = await _context.ProjectGroups
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(pm => pm.User)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return NotFound(new { Message = "Project not found" });

                // Check if current user has admin rights
                var currentUserMember = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == currentUserId);

                if (currentUserMember == null || currentUserMember.Role != ProjectRole.Admin)
                    return new ObjectResult(new { Message = "Only project admins can update member roles" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                // Find the member to update
                var memberToUpdate = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (memberToUpdate == null)
                    return NotFound(new { Message = "Member not found in project" });

                // Prevent removing the last admin by role change
                if (memberToUpdate.Role == ProjectRole.Admin &&
                    updateDto.Role != ProjectRole.Admin &&
                    project.ProjectMembers.Count(pm => pm.Role == ProjectRole.Admin) <= 1)
                {
                    return BadRequest(new { Message = "Cannot change role of the last admin" });
                }

                // Update the role
                memberToUpdate.Role = updateDto.Role;

                // Create notification for the updated user
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"Your role in project '{project.Name}' has been updated to {updateDto.Role}",
                    Type = NotificationType.TeamUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification
                await _notificationHub.Clients.Group(userId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return Ok(new
                {
                    Message = "Member role updated successfully",
                    Member = new
                    {
                        UserId = memberToUpdate.UserId,
                        FirstName = memberToUpdate.User.FirstName,
                        LastName = memberToUpdate.User.LastName,
                        Role = memberToUpdate.Role,
                        ProfilePicture = memberToUpdate.User.ProfilePicture
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating member role in project {projectId}");
                return StatusCode(500, new { Message = "An error occurred while updating the member role" });
            }
        }

        [HttpPost("{projectId}/tasks/{taskId}/attachments")]
        public async Task<IActionResult> AddTaskAttachment(int projectId, int taskId, IFormFile file)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.ProjectMembers)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return NotFound(new { Message = "Task not found" });

            // Check if user has access to the project
            var userMember = task.Project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId);

            if (userMember == null)
                return Forbid();

            // Process file upload
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "tasks");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                FileName = file.FileName,
                FilePath = fileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedById = userId
            };

            _context.TaskAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Attachment uploaded successfully", AttachmentId = attachment.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Get the project with its members
                var project = await _context.ProjectGroups
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (project == null)
                    return NotFound(new { Message = "Project not found" });

                // Check if user has admin rights in the project
                var userMember = project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null || userMember.Role != ProjectRole.Admin)
                    return new ObjectResult(new { Message = "Only project admins can delete projects" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                // Soft delete the project
                project.IsActive = false;
                //project.Status = ProjectStatus.Cancelled;

                // Create notifications for all project members
                var notifications = project.ProjectMembers.Select(member => new Notification
                {
                    UserId = member.UserId,
                    Message = $"Project '{project.Name}' has been deleted",
                    Type = NotificationType.ProjectUpdate,
                    ReferenceId = project.Id.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Send real-time notifications to all project members
                foreach (var member in project.ProjectMembers)
                {
                    var notification = notifications.First(n => n.UserId == member.UserId);
                    await _notificationHub.Clients.Group(member.UserId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new { Message = "Project deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting project {id}");
                return StatusCode(500, new { Message = "An error occurred while deleting the project" });
            }
        }


        private static IQueryable<ProjectGroup> ApplySorting(
            IQueryable<ProjectGroup> query,
            string? sortBy,
            string? sortDirection)
        {
            var isAscending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "name" => isAscending
                    ? query.OrderBy(p => p.Name)
                    : query.OrderByDescending(p => p.Name),
                "created" => isAscending
                    ? query.OrderBy(p => p.CreatedAt)
                    : query.OrderByDescending(p => p.CreatedAt),
                "progress" => isAscending
                    ? query.OrderBy(p => p.Tasks.Any()
                        ? (double)p.Tasks.Count(t => t.Status == TaskStatus.Completed) / p.Tasks.Count
                        : 0)
                    : query.OrderByDescending(p => p.Tasks.Any()
                        ? (double)p.Tasks.Count(t => t.Status == TaskStatus.Completed) / p.Tasks.Count
                        : 0),
                "tasks" => isAscending
                    ? query.OrderBy(p => p.Tasks.Count)
                    : query.OrderByDescending(p => p.Tasks.Count),
                "members" => isAscending
                    ? query.OrderBy(p => p.ProjectMembers.Count)
                    : query.OrderByDescending(p => p.ProjectMembers.Count),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        [HttpPut("milestones/{id}")]
        public async Task<IActionResult> UpdateMilestone(int id, [FromBody] MilestoneCreateDto milestoneDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Get milestone with project info to check permissions
                var milestone = await _context.ProjectMilestones
                    .Include(m => m.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (milestone == null)
                    return NotFound(new { Message = "Milestone not found" });

                // Check if user has rights to update milestone
                var userMember = milestone.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId &&
                        (pm.Role == ProjectRole.Admin || pm.Role == ProjectRole.Manager));

                if (userMember == null)
                    return Forbid();

                // Update milestone
                milestone.Title = milestoneDto.Title;
                milestone.Description = milestoneDto.Description;
                milestone.DueDate = milestoneDto.DueDate;
                milestone.Status = milestoneDto.Status;

                // Create notifications for all project members except the user performing the update
                var notifications = milestone.Project.ProjectMembers
                    .Where(pm => pm.UserId != userId) // Exclude the current user
                    .Select(member => new Notification
                    {
                        UserId = member.UserId,
                        Message = $"Milestone '{milestone.Title}' has been updated",
                        Type = NotificationType.ProjectUpdate,
                        ReferenceId = milestone.Project.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    })
                    .ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Send real-time notifications to all project members except the current user
                foreach (var notification in notifications)
                {
                    await _notificationHub.Clients.Group(notification.UserId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new { Message = "Milestone updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating milestone {id}");
                return StatusCode(500, new { Message = "An error occurred while updating the milestone" });
            }
        }

        [HttpDelete("milestones/{id}")]
        public async Task<IActionResult> DeleteMilestone(int id)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Get milestone with project info to check permissions
                var milestone = await _context.ProjectMilestones
                    .Include(m => m.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (milestone == null)
                    return NotFound(new { Message = "Milestone not found" });

                // Check if user has rights to delete milestone
                var userMember = milestone.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId &&
                        (pm.Role == ProjectRole.Admin || pm.Role == ProjectRole.Manager));

                if (userMember == null)
                    return Forbid();

                // Store project ID and title for notification before deletion
                var projectId = milestone.Project.Id;
                var milestoneTitle = milestone.Title;

                // Create notifications for all project members except the user performing the deletion
                var notifications = milestone.Project.ProjectMembers
                    .Where(pm => pm.UserId != userId) // Exclude the current user
                    .Select(member => new Notification
                    {
                        UserId = member.UserId,
                        Message = $"Milestone '{milestoneTitle}' has been deleted",
                        Type = NotificationType.ProjectUpdate,
                        ReferenceId = projectId.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    })
                    .ToList();

                // Delete milestone
                _context.ProjectMilestones.Remove(milestone);

                // Add notifications
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Send real-time notifications to all project members except the current user
                foreach (var notification in notifications)
                {
                    await _notificationHub.Clients.Group(notification.UserId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new { Message = "Milestone deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting milestone {id}");
                return StatusCode(500, new { Message = "An error occurred while delAddeting the milestone" });
            }
        
        }
    }
}

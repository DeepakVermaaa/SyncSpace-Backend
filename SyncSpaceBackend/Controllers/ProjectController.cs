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
                .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId));

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
                .FirstOrDefaultAsync(p => p.Id == id &&
                    p.ProjectMembers.Any(pm => pm.UserId == userId));

            if (project == null)
                return NotFound(new { Message = "Project not found" });

            var projectDetails = new
            {
                project.Id,
                project.Name,
                project.Description,
                project.CreatedAt,
                CreatedBy = new { project.CreatedBy.FirstName, project.CreatedBy.LastName },
                Members = project.ProjectMembers.Select(pm => new
                {
                    UserId = pm.UserId,
                    pm.User.FirstName,
                    pm.User.LastName,
                    pm.Role,
                    pm.JoinedAt
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
            await _context.SaveChangesAsync();

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
                    JoinedAt = DateTime.UtcNow
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
                        JoinedAt = newMember.JoinedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding member to project {id}");
                return StatusCode(500, new { Message = "An error occurred while adding the member" });
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
    }
}

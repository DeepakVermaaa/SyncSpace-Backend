using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.DTO;
using SyncSpaceBackend.Hubs;
using SyncSpaceBackend.Models;
using System.Security.Claims;
using WebAPI.Context;
using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ILogger<TaskController> _logger;
        private readonly IWebHostEnvironment _environment;

        public TaskController(
            AppDbContext context,
            IHubContext<NotificationHub> notificationHub,
            ILogger<TaskController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _notificationHub = notificationHub;
            _logger = logger;
            _environment = environment;
        }

        // Get Tasks for List View
        //[HttpGet]
        //public async Task<IActionResult> GetTasks([FromQuery] TaskFilterDto filterParams)
        //{
        //    try
        //    {
        //        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userId = Convert.ToInt32(userIdString);

        //        var query = _context.ProjectTasks
        //            .Include(t => t.Project)
        //                .ThenInclude(p => p.ProjectMembers)
        //            .Include(t => t.AssignedTo)
        //            .Include(t => t.CreatedBy)
        //            .Include(t => t.Comments)
        //            .Include(t => t.Attachments)
        //            .Where(t => t.Project.ProjectMembers.Any(pm => pm.UserId == userId));

        //        // Apply filters
        //        query = ApplyTaskFilters(query, filterParams, userId);

        //        // Apply sorting
        //        query = ApplySorting(query, filterParams.SortBy, filterParams.SortDirection);

        //        var totalCount = await query.CountAsync();

        //        var tasks = await query
        //            .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
        //            .Take(filterParams.PageSize)
        //            .Select(t => new
        //            {
        //                t.Id,
        //                t.Title,
        //                t.Description,
        //                t.Status,
        //                t.Priority,
        //                t.DueDate,
        //                t.CreatedAt,
        //                Project = new { t.Project.Id, t.Project.Name },
        //                AssignedTo = new
        //                {
        //                    t.AssignedTo.Id,
        //                    t.AssignedTo.FirstName,
        //                    t.AssignedTo.LastName,
        //                    t.AssignedTo.ProfilePicture
        //                },
        //                CreatedBy = new
        //                {
        //                    t.CreatedBy.Id,
        //                    t.CreatedBy.FirstName,
        //                    t.CreatedBy.LastName
        //                },
        //                CommentsCount = t.Comments.Count,
        //                AttachmentsCount = t.Attachments.Count,
        //                IsOverdue = t.DueDate < DateTime.UtcNow && t.Status != TaskStatusEnum.Completed
        //            })
        //            .ToListAsync();

        //        return Ok(new
        //        {
        //            Data = tasks,
        //            TotalCount = totalCount,
        //            PageSize = filterParams.PageSize,
        //            PageNumber = filterParams.PageNumber,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting tasks");
        //        return StatusCode(500, new { Message = "An error occurred while fetching tasks" });
        //    }
        //}

        //// Get Tasks for Kanban View
        //[HttpGet("kanban")]
        //public async Task<IActionResult> GetKanbanTasks([FromQuery] int projectId)
        //{
        //    try
        //    {
        //        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userId = Convert.ToInt32(userIdString);

        //        // Verify project access
        //        var hasAccess = await _context.ProjectMembers
        //            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        //        if (!hasAccess)
        //            return Forbid();

        //        var tasks = await _context.ProjectTasks
        //            .Include(t => t.AssignedTo)
        //            .Include(t => t.Comments)
        //            .Include(t => t.Attachments)
        //            .Where(t => t.ProjectId == projectId)
        //            .Select(t => new
        //            {
        //                t.Id,
        //                t.Title,
        //                t.Description,
        //                t.Status,
        //                t.Priority,
        //                t.DueDate,
        //                AssignedTo = new
        //                {
        //                    t.AssignedTo.Id,
        //                    t.AssignedTo.FirstName,
        //                    t.AssignedTo.LastName,
        //                    t.AssignedTo.ProfilePicture
        //                },
        //                CommentsCount = t.Comments.Count,
        //                AttachmentsCount = t.Attachments.Count,
        //                IsOverdue = t.DueDate < DateTime.UtcNow && t.Status != TaskStatusEnum.Completed
        //            })
        //            .ToListAsync();

        //        var groupedTasks = new
        //        {
        //            Todo = tasks.Where(t => t.Status == TaskStatusEnum.Todo).ToList(),
        //            InProgress = tasks.Where(t => t.Status == TaskStatusEnum.InProgress).ToList(),
        //            UnderReview = tasks.Where(t => t.Status == TaskStatusEnum.UnderReview).ToList(),
        //            Completed = tasks.Where(t => t.Status == TaskStatusEnum.Completed).ToList()
        //        };

        //        return Ok(groupedTasks);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting Kanban tasks");
        //        return StatusCode(500, new { Message = "An error occurred while fetching tasks" });
        //    }
        //}


        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] TaskFilterDto filterParams, [FromQuery] string view = "list")
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                if (filterParams.ProjectId.HasValue)
                {
                    var hasAccess = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == filterParams.ProjectId && pm.UserId == userId);

                    if (!hasAccess)
                        return Forbid();
                }

                var query = _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.Comments)
                    .Include(t => t.Attachments)
                    .Where(t => t.Project.ProjectMembers.Any(pm => pm.UserId == userId));

                // Apply filters
                query = ApplyTaskFilters(query, filterParams, userId);

                // Apply sorting
                query = ApplySorting(query, filterParams.SortBy, filterParams.SortDirection);

                // Select common task fields
                var baseQuery = query.Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.DueDate,
                    t.CreatedAt,
                    AssignedTo = new
                    {
                        t.AssignedTo.Id,
                        t.AssignedTo.FirstName,
                        t.AssignedTo.LastName,
                        t.AssignedTo.ProfilePicture
                    },
                    CommentsCount = t.Comments.Count,
                    AttachmentsCount = t.Attachments.Count,
                    IsOverdue = t.DueDate < DateTime.UtcNow && t.Status != TaskStatusEnum.Completed
                });

                // Return data based on view type
                if (view.ToLower() == "kanban")
                {
                    var tasks = await baseQuery.ToListAsync();
                    return Ok(new
                    {
                        Todo = tasks.Where(t => t.Status == TaskStatusEnum.Todo),
                        InProgress = tasks.Where(t => t.Status == TaskStatusEnum.InProgress),
                        UnderReview = tasks.Where(t => t.Status == TaskStatusEnum.UnderReview),
                        Completed = tasks.Where(t => t.Status == TaskStatusEnum.Completed)
                    });
                }
                else // list view
                {
                    var totalCount = await query.CountAsync();
                    var tasks = await baseQuery
                        .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                        .Take(filterParams.PageSize)
                        .ToListAsync();

                    return Ok(new
                    {
                        Data = tasks,
                        TotalCount = totalCount,
                        PageSize = filterParams.PageSize,
                        PageNumber = filterParams.PageNumber,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return StatusCode(500, new { Message = "An error occurred while fetching tasks" });
            }
        }

        // Get Single Task Details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.CreatedBy)
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.UploadedBy)
                    .FirstOrDefaultAsync(t => t.Id == id &&
                        t.Project.ProjectMembers.Any(pm => pm.UserId == userId));

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var taskDetails = new
                {
                    task.Id,
                    task.Title,
                    task.Description,
                    task.Status,
                    task.Priority,
                    task.DueDate,
                    task.CreatedAt,
                    Project = new { task.Project.Id, task.Project.Name },
                    AssignedTo = new
                    {
                        task.AssignedTo.Id,
                        task.AssignedTo.FirstName,
                        task.AssignedTo.LastName,
                        task.AssignedTo.ProfilePicture
                    },
                    CreatedBy = new
                    {
                        task.CreatedBy.Id,
                        task.CreatedBy.FirstName,
                        task.CreatedBy.LastName
                    },
                    Comments = task.Comments
                            .OrderByDescending(c => c.CreatedAt)
                            .Select(c => new
                               {
                                c.Id,
                                c.Content,
                                c.CreatedAt,
                                CreatedBy = new
                                  {
                                   c.CreatedBy.Id,
                                   c.CreatedBy.FirstName,
                                   c.CreatedBy.LastName,
                                   c.CreatedBy.ProfilePicture
                                  }
                                 }),
                    Attachments = task.Attachments.Select(a => new
                    {
                        a.Id,
                        a.FileName,
                        a.FileType,
                        a.FileSize,
                        a.UploadedAt,
                        UploadedBy = new
                        {
                            a.UploadedBy.Id,
                            a.UploadedBy.FirstName,
                            a.UploadedBy.LastName
                        }
                    })
                };

                return Ok(taskDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting task {id}");
                return StatusCode(500, new { Message = "An error occurred while fetching the task" });
            }
        }

        // Create New Task
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Verify project access
                var hasAccess = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == taskDto.ProjectId && pm.UserId == userId);

                if (!hasAccess)
                    return Forbid();

                var task = new ProjectTask
                {
                    ProjectId = taskDto.ProjectId,
                    Title = taskDto.Title,
                    Description = taskDto.Description,
                    Status = taskDto.Status,
                    Priority = taskDto.Priority,
                    DueDate = taskDto.DueDate,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    AssignedToId = taskDto.AssignedToId
                };

                _context.ProjectTasks.Add(task);

                // Create notification for assigned user
                if (taskDto.AssignedToId != userId)
                {
                    var notification = new Notification
                    {
                        UserId = taskDto.AssignedToId,
                        Message = $"You have been assigned to task '{taskDto.Title}'",
                        Type = NotificationType.TaskAssigned,
                        ReferenceId = task.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                // Send notification
                if (taskDto.AssignedToId != userId)
                {
                    await _notificationHub.Clients.Group(taskDto.AssignedToId.ToString())
                        .SendAsync("ReceiveNotification", new
                        {
                            Message = $"You have been assigned to task '{taskDto.Title}'",
                            Type = NotificationType.TaskAssigned
                        });
                }

                return Ok(new { Message = "Task created successfully", TaskId = task.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { Message = "An error occurred while creating the task" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto taskDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                // Check if user has access to the project
                var userMember = task.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null)
                    return Forbid();

                // Track changes for notification
                var changes = new List<string>();
                if (task.Title != taskDto.Title) changes.Add("title");
                if (task.Description != taskDto.Description) changes.Add("description");
                if (task.Status != taskDto.Status) changes.Add("status");
                if (task.Priority != taskDto.Priority) changes.Add("priority");
                if (task.DueDate != taskDto.DueDate) changes.Add("due date");

                var originalAssignee = task.AssignedToId;

                // Update task properties
                task.Title = taskDto.Title;
                task.Description = taskDto.Description;
                task.Status = taskDto.Status;
                task.Priority = taskDto.Priority;
                task.DueDate = taskDto.DueDate;
                task.AssignedToId = taskDto.AssignedToId;

                await _context.SaveChangesAsync();

                // Handle notifications
                if (taskDto.AssignedToId != originalAssignee)
                {
                    // Notify new assignee
                    var assigneeNotification = new Notification
                    {
                        UserId = taskDto.AssignedToId,
                        Message = $"You have been assigned to task '{task.Title}'",
                        Type = NotificationType.TaskAssigned,
                        ReferenceId = task.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(assigneeNotification);
                    await _context.SaveChangesAsync();

                    await _notificationHub.Clients.Group(taskDto.AssignedToId.ToString())
                        .SendAsync("ReceiveNotification", assigneeNotification);

                    // Notify previous assignee if there was one
                    if (originalAssignee != 0)
                    {
                        var previousAssigneeNotification = new Notification
                        {
                            UserId = originalAssignee,
                            Message = $"Task '{task.Title}' has been reassigned to someone else",
                            Type = NotificationType.TaskUpdated,
                            ReferenceId = task.Id.ToString(),
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };

                        _context.Notifications.Add(previousAssigneeNotification);
                        await _context.SaveChangesAsync();

                        await _notificationHub.Clients.Group(originalAssignee.ToString())
                            .SendAsync("ReceiveNotification", previousAssigneeNotification);
                    }
                }
                else if (changes.Any() && task.AssignedToId != userId)
                {
                    // Notify current assignee about other changes
                    var changesList = string.Join(", ", changes);
                    var notification = new Notification
                    {
                        UserId = task.AssignedToId,
                        Message = $"Task '{task.Title}' has been updated ({changesList})",
                        Type = NotificationType.TaskUpdated,
                        ReferenceId = task.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    await _notificationHub.Clients.Group(task.AssignedToId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new
                {
                    Message = "Task updated successfully",
                    Task = new
                    {
                        task.Id,
                        task.Title,
                        task.Description,
                        task.Status,
                        task.Priority,
                        task.DueDate,
                        task.AssignedToId,
                        Changes = changes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task {id}");
                return StatusCode(500, new { Message = "An error occurred while updating the task" });
            }
        }

        // Update Task Status (for Kanban drag-and-drop)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatusUpdateDto updateDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var userMember = task.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null)
                    return Forbid();

                var oldStatus = task.Status;
                task.Status = (TaskStatusEnum)updateDto.NewStatus;

                await _context.SaveChangesAsync();

                // Notify task assignee about status change
                if (task.AssignedToId != userId)
                {
                    var notification = new Notification
                    {
                        UserId = task.AssignedToId,
                        Message = $"Task '{task.Title}' status changed from {oldStatus} to {task.Status}",
                        Type = NotificationType.TaskUpdated,
                        ReferenceId = task.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    await _notificationHub.Clients.Group(task.AssignedToId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new { Message = "Task status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task status {id}");
                return StatusCode(500, new { Message = "An error occurred while updating task status" });
            }
        }

        // Add Comment to Task
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CommentCreateDto commentDto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var userMember = task.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null)
                    return Forbid();

                var comment = new TaskComment
                {
                    TaskId = id,
                    Content = commentDto.Content,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };

                _context.TaskComments.Add(comment);

                // Notify task assignee about new comment
                if (task.AssignedToId != userId)
                {
                    var notification = new Notification
                    {
                        UserId = task.AssignedToId,
                        Message = $"New comment on task '{task.Title}'",
                        Type = NotificationType.TaskUpdated,
                        ReferenceId = task.Id.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Comment added successfully", CommentId = comment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment to task {id}");
                return StatusCode(500, new { Message = "An error occurred while adding the comment" });
            }
        }

        // Upload Task Attachment
        [HttpPost("{id}/attachments")]
        public async Task<IActionResult> AddAttachment(int id, IFormFile file)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var userMember = task.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null)
                    return Forbid();

                // Process file upload
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "tasks");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new TaskAttachment
                {
                    TaskId = id,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading attachment to task {id}");
                return StatusCode(500, new { Message = "An error occurred while uploading the attachment" });
            }
        }


        // Remove Task Attachment
        [HttpDelete("{taskId}/attachments/{attachmentId}")]
        public async Task<IActionResult> RemoveAttachment(int taskId, int attachmentId)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);

                // Get the task and verify user has access
                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var userMember = task.Project.ProjectMembers
                    .FirstOrDefault(pm => pm.UserId == userId);

                if (userMember == null)
                    return Forbid();

                // Get the attachment
                var attachment = await _context.TaskAttachments
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);

                if (attachment == null)
                    return NotFound(new { Message = "Attachment not found" });

                // Delete the physical file
                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "tasks", attachment.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Remove from database
                _context.TaskAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Attachment removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing attachment {attachmentId} from task {taskId}");
                return StatusCode(500, new { Message = "An error occurred while removing the attachment" });
            }
        }

        // Helper Methods
        private static IQueryable<ProjectTask> ApplyTaskFilters(
            IQueryable<ProjectTask> query,
            TaskFilterDto filterParams,
            int userId)
        {
            if (filterParams.ProjectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == filterParams.ProjectId);
            }

            if (filterParams.Status.HasValue)
            {
                query = query.Where(t => t.Status == filterParams.Status);
            }

            if (filterParams.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == filterParams.Priority);
            }

            if (filterParams.AssignedToMe)
            {
                query = query.Where(t => t.AssignedToId == userId);
            }

            if (filterParams.CreatedByMe)
            {
                query = query.Where(t => t.CreatedById == userId);
            }

            if (!string.IsNullOrEmpty(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm));
            }

            if (filterParams.DueDateFrom.HasValue)
            {
                query = query.Where(t => t.DueDate >= filterParams.DueDateFrom);
            }

            if (filterParams.DueDateTo.HasValue)
            {
                query = query.Where(t => t.DueDate <= filterParams.DueDateTo);
            }

            return query;
        }

        private static IQueryable<ProjectTask> ApplySorting(
            IQueryable<ProjectTask> query,
            string? sortBy,
            string? sortDirection)
        {
            var isAscending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "title" => isAscending
                    ? query.OrderBy(t => t.Title)
                    : query.OrderByDescending(t => t.Title),
                "duedate" => isAscending
                    ? query.OrderBy(t => t.DueDate)
                    : query.OrderByDescending(t => t.DueDate),
                "priority" => isAscending
                    ? query.OrderBy(t => t.Priority)
                    : query.OrderByDescending(t => t.Priority),
                "status" => isAscending
                    ? query.OrderBy(t => t.Status)
                    : query.OrderByDescending(t => t.Status),
                "created" => isAscending
                    ? query.OrderBy(t => t.CreatedAt)
                    : query.OrderByDescending(t => t.CreatedAt),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebAPI.Context;
using WebAPI.Models;
using System.Text.RegularExpressions;
using System.Net;
using Microsoft.AspNetCore.Http;
using static SyncSpaceBackend.Enums.Enum;
using SyncSpaceBackend.Hubs;
using SyncSpaceBackend.Models;
using Mammoth;

namespace WebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ILogger<DocumentsController> _logger;
        private const long MaxFileSize = 100 * 1024 * 1024; // 100MB
        private readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };

        public DocumentsController(
            AppDbContext dbContext,
            IWebHostEnvironment hostEnvironment,
            IHubContext<NotificationHub> notificationHub,
            ILogger<DocumentsController> logger)
        {
            _dbContext = dbContext;
            _hostEnvironment = hostEnvironment;
            _notificationHub = notificationHub;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetDocuments([FromQuery] DocumentFilterDto filterParams)
        {
            try
            {
                var userId = GetCurrentUserId();
                var query = _dbContext.Documents.AsQueryable();

                // Get documents from all projects user is member of
                var userProjectIds = await _dbContext.ProjectMembers
                    .Where(pm => pm.UserId == userId)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                query = query.Where(d => userProjectIds.Contains(d.ProjectGroupId));

                // Apply common filters
                query = query.Where(d => !d.IsDeleted);

                if (!string.IsNullOrWhiteSpace(filterParams.SearchQuery))
                {
                    var searchTerm = filterParams.SearchQuery.ToLower();
                    query = query.Where(d =>
                        d.Name.ToLower().Contains(searchTerm) ||
                        d.Description.ToLower().Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(filterParams.FileType))
                {
                    query = query.Where(d => d.FileExtension.ToLower() == filterParams.FileType.ToLower());
                }

                // Apply includes
                var queryWithIncludes = query
                    .Include(d => d.UploadedBy)
                    .Include(d => d.Project)
                    .Include(d => d.Versions)
                    .Include(d => d.Permissions);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var documents = await queryWithIncludes
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Description,
                        d.FileExtension,
                        d.FileSize,
                        d.UploadedAt,
                        Project = new
                        {
                            d.Project.Id,
                            d.Project.Name
                        },
                        UploadedBy = new
                        {
                            d.UploadedBy.FirstName,
                            d.UploadedBy.LastName,
                            d.UploadedBy.ProfilePicture
                        },
                        LatestVersion = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().VersionNumber,
                        Permissions = d.Permissions.Select(p => new
                        {
                            p.UserId,
                            p.PermissionLevel
                        })
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Data = documents,
                    TotalCount = totalCount,
                    PageSize = filterParams.PageSize,
                    PageNumber = filterParams.PageNumber,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { Message = "An error occurred while retrieving documents" });
            }
        }

        [HttpGet("project/{projectId?}")]
        public async Task<ActionResult<IEnumerable<Document>>> GetProjectDocuments(int? projectId,
            [FromQuery] DocumentFilterDto filterParams)
        {
            try
            {
                var userId = GetCurrentUserId();
                var query = _dbContext.Documents.AsQueryable();

                if (projectId.HasValue)
                {
                    // Check project membership if projectId is provided
                    var isMember = await _dbContext.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
                    if (!isMember)
                        return Forbid();

                    query = query.Where(d => d.ProjectGroupId == projectId);
                }
                else
                {
                    // If no projectId, get documents from all projects user is member of
                    var userProjectIds = await _dbContext.ProjectMembers
                        .Where(pm => pm.UserId == userId)
                        .Select(pm => pm.ProjectId)
                        .ToListAsync();

                    query = query.Where(d => userProjectIds.Contains(d.ProjectGroupId));
                }

                query = query.Where(d => !d.IsDeleted);

                if (!string.IsNullOrWhiteSpace(filterParams.SearchQuery))
                {
                    var searchTerm = filterParams.SearchQuery.ToLower();
                    query = query.Where(d =>
                        d.Name.ToLower().Contains(searchTerm) ||
                        d.Description.ToLower().Contains(searchTerm));
                }

                // Apply file type filter
                if (!string.IsNullOrWhiteSpace(filterParams.FileType))
                {
                    query = query.Where(d => d.FileExtension.ToLower() == filterParams.FileType.ToLower());
                }

                // Apply includes after all filters
                var queryWithIncludes = query
                    .Include(d => d.UploadedBy)
                    .Include(d => d.Project)
                    .Include(d => d.Versions)
                    .Include(d => d.Permissions);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var documents = await queryWithIncludes
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Description,
                        d.FileExtension,
                        d.FileSize,
                        d.UploadedAt,
                        Project = new
                        {
                            d.Project.Id,
                            d.Project.Name
                        },
                        UploadedBy = new
                        {
                            d.UploadedBy.FirstName,
                            d.UploadedBy.LastName,
                            d.UploadedBy.ProfilePicture
                        },
                        LatestVersion = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().VersionNumber,
                        Permissions = d.Permissions.Select(p => new
                        {
                            p.UserId,
                            p.PermissionLevel
                        })
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Data = documents,
                    TotalCount = totalCount,
                    PageSize = filterParams.PageSize,
                    PageNumber = filterParams.PageNumber,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for project {ProjectId}", projectId);
                return StatusCode(500, new { Message = "An error occurred while retrieving documents" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.UploadedBy)
                    .Include(d => d.Versions)
                        .ThenInclude(v => v.UploadedBy)
                    .Include(d => d.Permissions)
                        .ThenInclude(p => p.User)
                    .Include(d => d.Project)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (document == null)
                    return NotFound(new { Message = "Document not found" });

                // Check document permissions
                var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (permission == null)
                    return Forbid();

                // Get the latest version number
                var latestVersion = document.Versions.Max(v => v.VersionNumber);

                var result = new
                {
                    document.Id,
                    document.Name,
                    document.Description,
                    document.FileExtension,
                    document.FileSize,
                    document.UploadedAt,
                    document.CurrentVersionId,
                    LatestVersion = latestVersion,
                    Project = new
                    {
                        document.Project.Id,
                        document.Project.Name
                    },
                    UploadedBy = new
                    {
                        document.UploadedBy.Id,
                        document.UploadedBy.FirstName,
                        document.UploadedBy.LastName,
                        document.UploadedBy.ProfilePicture
                    },
                    Versions = document.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new
                    {
                        v.Id,
                        v.VersionNumber,
                        v.FileSize,
                        v.FileExtension,
                        v.UploadedAt,
                        v.Comment,
                        UploadedBy = new
                        {
                            v.UploadedBy.FirstName,
                            v.UploadedBy.LastName,
                            v.UploadedBy.ProfilePicture
                        }
                    }),
                    Permissions = document.Permissions.Select(p => new
                    {
                        p.UserId,
                        p.PermissionLevel,
                        User = new
                        {
                            p.User.FirstName,
                            p.User.LastName,
                            p.User.Email
                        }
                    }),
                    CurrentUserPermission = permission.PermissionLevel
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the document" });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id, [FromQuery] int? versionId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Permissions)
                    .Include(d => d.Versions)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (document == null)
                    return NotFound(new { Message = "Document not found" });

                // Check download permissions
                var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (permission == null || permission.PermissionLevel < DocumentPermissionLevel.Download)
                    return Forbid();

                // Determine which version to download
                var version = versionId.HasValue
                    ? document.Versions.FirstOrDefault(v => v.Id == versionId)
                    : document.Versions.FirstOrDefault(v => v.Id == document.CurrentVersionId);

                if (version == null)
                    return NotFound(new { Message = "Version not found" });

                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, version.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { Message = "File not found" });

                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;

                // Handle Range header for resumable downloads
                var (rangeStart, rangeEnd) = GetRangeFromRequest(fileSize);
                Response.Headers.Add("Accept-Ranges", "bytes");

                // If range is specified, return partial content
                if (rangeStart.HasValue)
                {
                    Response.StatusCode = StatusCodes.Status206PartialContent;
                    Response.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{fileSize}");
                    Response.Headers.Add("Content-Length", (rangeEnd - rangeStart + 1).ToString());
                }
                else
                {
                    Response.Headers.Add("Content-Length", fileSize.ToString());
                }

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{WebUtility.UrlEncode(document.Name + document.FileExtension)}\"");
                Response.Headers.Add("Cache-Control", "private, max-age=0, must-revalidate");

                return new FileCallbackResult(
    GetContentType(document.FileExtension),
    async (outputStream, _) =>
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (rangeStart.HasValue)
            {
                fileStream.Seek(rangeStart.Value, SeekOrigin.Begin);
                var remainingBytes = (rangeEnd!.Value - rangeStart.Value + 1);
                await CopyStreamByChunks(fileStream, outputStream, remainingBytes);
            }
            else
            {
                await CopyStreamByChunks(fileStream, outputStream, fileSize);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming file {FilePath}", filePath);
            throw;
        }
    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating download for document {DocumentId}", id);
                return StatusCode(500, new { Message = "An error occurred while downloading the document" });
            }
        }


        [HttpPost]
        public async Task<ActionResult<Document>> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Validate project membership
                var isMember = await _dbContext.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == userId);

                if (!isMember)
                    return Forbid();

                // Validate file
                if (!ValidateFile(request.File))
                    return BadRequest(new { Message = "Invalid file. Check size and file type." });

                var uniqueFileName = GetUniqueFileName(request.File.FileName);
                var filePath = await SaveFile(request.File, uniqueFileName);

                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var document = new Document
                    {
                        Name = request.Name ?? request.File.FileName,
                        Description = request.Description,
                        FilePath = filePath,
                        FileExtension = Path.GetExtension(request.File.FileName),
                        FileSize = request.File.Length,
                        UploadedAt = DateTime.Now,
                        UploadedById = userId,
                        ProjectGroupId = request.ProjectId,
                        IsDeleted = false
                    };

                    _dbContext.Documents.Add(document);
                    await _dbContext.SaveChangesAsync();

                    var documentVersion = new DocumentVersion
                    {
                        DocumentId = document.Id,
                        VersionNumber = 1,
                        FilePath = filePath,
                        FileExtension = document.FileExtension,
                        FileSize = document.FileSize,
                        UploadedAt = document.UploadedAt,
                        UploadedById = userId,
                        Comment = request.Comment ?? "Initial upload"
                    };

                    _dbContext.DocumentVersions.Add(documentVersion);
                    await _dbContext.SaveChangesAsync();

                    document.CurrentVersionId = documentVersion.Id;

                    // Add owner permission
                    var permission = new DocumentPermission
                    {
                        DocumentId = document.Id,
                        UserId = userId,
                        PermissionLevel = DocumentPermissionLevel.Owner
                    };

                    _dbContext.DocumentPermissions.Add(permission);

                    // Create notification for project members
                    var projectMembers = await _dbContext.ProjectMembers
                        .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId != userId)
                        .Select(pm => pm.UserId)
                        .ToListAsync();

                    var notifications = projectMembers.Select(memberId => new Notification
                    {
                        UserId = memberId,
                        Message = $"New document '{document.Name}' has been uploaded to the project",
                        Type = NotificationType.ProjectUpdate,
                        ReferenceId = document.Id.ToString(),
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    }).ToList();

                    _dbContext.Notifications.AddRange(notifications);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Send real-time notifications
                    foreach (var notification in notifications)
                    {
                        await _notificationHub.Clients.Group(notification.UserId.ToString())
                            .SendAsync("ReceiveNotification", notification);
                    }

                    return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { Message = "An error occurred while uploading the document" });
            }
        }


        [HttpPut("{id}/version")]
        public async Task<IActionResult> UpdateDocument(int id, [FromForm] DocumentUpdateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Versions)
                    .Include(d => d.Permissions)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (document == null)
                    return NotFound();

                // Check edit permissions
                var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (permission == null || permission.PermissionLevel < DocumentPermissionLevel.Edit)
                    return Forbid();

                // Validate file
                if (!ValidateFile(request.File))
                    return BadRequest("Invalid file. Check size and file type.");

                var uniqueFileName = GetUniqueFileName(request.File.FileName);
                var filePath = await SaveFile(request.File, uniqueFileName);

                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var newVersion = new DocumentVersion
                    {
                        DocumentId = document.Id,
                        VersionNumber = document.Versions.Count + 1,
                        FilePath = filePath,
                        FileExtension = Path.GetExtension(request.File.FileName),
                        FileSize = request.File.Length,
                        UploadedAt = DateTime.Now,
                        UploadedById = userId,
                        Comment = request.Comment ?? $"Version {document.Versions.Count + 1}"
                    };

                    _dbContext.DocumentVersions.Add(newVersion);
                    await _dbContext.SaveChangesAsync();

                    document.FilePath = filePath;
                    document.FileExtension = newVersion.FileExtension;
                    document.FileSize = newVersion.FileSize;
                    document.CurrentVersionId = newVersion.Id;

                    _dbContext.Documents.Update(document);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", id);
                return StatusCode(500, "An error occurred while updating the document");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Permissions)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                    return NotFound();

                // Check delete permissions
                var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (permission?.PermissionLevel != DocumentPermissionLevel.Owner)
                    return Forbid();

                document.IsDeleted = true;
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return StatusCode(500, "An error occurred while deleting the document");
            }
        }

        [HttpPut("{id}/permissions")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdatePermissionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Permissions)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                    return NotFound(new { Message = "Document not found" });

                // Check if user has owner permissions
                var userPermission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (userPermission?.PermissionLevel != DocumentPermissionLevel.Owner)
                    return Forbid();

                // Update or create permission
                var targetPermission = document.Permissions
                    .FirstOrDefault(p => p.UserId == request.UserId);

                if (targetPermission != null)
                {
                    targetPermission.PermissionLevel = request.PermissionLevel;
                }
                else
                {
                    targetPermission = new DocumentPermission
                    {
                        DocumentId = id,
                        UserId = request.UserId,
                        PermissionLevel = request.PermissionLevel
                    };
                    _dbContext.DocumentPermissions.Add(targetPermission);
                }

                // Create notification
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Message = $"Your permissions for document '{document.Name}' have been updated to {request.PermissionLevel}",
                    Type = NotificationType.ProjectUpdate,
                    ReferenceId = document.Id.ToString(),
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send real-time notification
                await _notificationHub.Clients.Group(request.UserId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return Ok(new { Message = "Permissions updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for document {DocumentId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating permissions" });
            }
        }

        [HttpDelete("{id}/permissions/{userId}")]
        public async Task<IActionResult> RemovePermission(int id, int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Permissions)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                    return NotFound(new { Message = "Document not found" });

                // Check if current user has owner permissions
                var currentUserPermission = document.Permissions
                    .FirstOrDefault(p => p.UserId == currentUserId);
                if (currentUserPermission?.PermissionLevel != DocumentPermissionLevel.Owner)
                    return Forbid();

                // Cannot remove owner's permission
                var targetPermission = document.Permissions
                    .FirstOrDefault(p => p.UserId == userId);
                if (targetPermission == null)
                    return NotFound(new { Message = "Permission not found" });
                if (targetPermission.PermissionLevel == DocumentPermissionLevel.Owner)
                    return BadRequest(new { Message = "Cannot remove owner's permission" });

                // Remove the permission
                _dbContext.DocumentPermissions.Remove(targetPermission);

                // Create notification for the user
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"Your access to document '{document.Name}' has been removed",
                    Type = NotificationType.ProjectUpdate,
                    ReferenceId = document.Id.ToString(),
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send real-time notification
                await _notificationHub.Clients.Group(userId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return Ok(new { Message = "Permission removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission for document {DocumentId}", id);
                return StatusCode(500, new { Message = "An error occurred while removing the permission" });
            }
        }

        /// <summary>
        /// Previews a document with optional format conversion
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <param name="versionId">Optional version ID</param>
        /// <param name="format">Format type (raw or html)</param>
        /// <returns>Document preview content</returns>
        /// <response code="200">Returns the document preview</response>
        /// <response code="404">Document not found</response>
        /// <response code="403">Unauthorized access</response>
        [HttpGet("{id}/preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Produces("application/octet-stream", "text/html", "application/pdf")]
        public async Task<IActionResult> PreviewDocument(int id, [FromQuery] int? versionId = null,
            [FromQuery] string format = "raw")
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _dbContext.Documents
                    .Include(d => d.Permissions)
                    .Include(d => d.Versions)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (document == null)
                    return NotFound(new { Message = "Document not found" });

                //Check view permissions
                var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
                if (permission == null)
                    return Forbid();

                //Determine which version to preview
                var version = versionId.HasValue
                    ? document.Versions.FirstOrDefault(v => v.Id == versionId)
                    : document.Versions.FirstOrDefault(v => v.Id == document.CurrentVersionId);

                if (version == null)
                    return NotFound(new { Message = "Version not found" });

                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, version.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { Message = "File not found" });

                //If format is HTML and it's a Word document, convert to HTML
                if (format?.ToLower() == "html" &&
                    (document.FileExtension.ToLower() == ".doc" || document.FileExtension.ToLower() == ".docx"))
                {
                    try
                    {
                        var html = ConvertWordToHtml(filePath);
                        return Content(html, "text/html");
                    }
                    catch (NotSupportedException ex)
                    {
                        _logger.LogWarning(ex, "Unsupported document format for HTML preview {DocumentId}", id);
                        return StatusCode(415, new { Message = ex.Message });
                    }
                    catch (InvalidDataException ex)
                    {
                        _logger.LogError(ex, "Invalid document format {DocumentId}", id);
                        return StatusCode(422, new { Message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting Word document to HTML {DocumentId}", id);
                        return StatusCode(500, new { Message = "Failed to generate preview" });
                    }
                }

                //For other formats or raw preview, return the file as is
                Response.Headers.Add("Content-Disposition",
                    $"inline; filename=\"{WebUtility.UrlEncode(document.Name + document.FileExtension)}\"");

                return new FileStreamResult(
                    new FileStream(filePath, FileMode.Open, FileAccess.Read),
                    GetContentType(document.FileExtension));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing document {DocumentId}", id);
                return StatusCode(500, new { Message = "An error occurred while previewing the document" });
            }
        }

        private string ConvertWordToHtml(string filePath)
        {
            try
            {
                // First check if it's really a .docx file
                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".doc")
                {
                    throw new NotSupportedException("Old .doc format is not supported. Please convert to .docx");
                }

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Verify file header
                    byte[] header = new byte[4];
                    fileStream.Read(header, 0, 4);
                    fileStream.Position = 0; // Reset position

                    // Check for ZIP file signature (PK\x03\x04)
                    if (header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
                    {
                        throw new InvalidDataException("File is not a valid .docx document");
                    }

                    var converter = new DocumentConverter();

                    try
                    {
                        var result = converter.ConvertToHtml(fileStream);
                        var html = result.Value;

                        // Log any warnings
                        foreach (var warning in result.Warnings)
                        {
                            _logger.LogWarning("Document conversion warning: {Warning}", warning);
                        }

                        // Add basic styling
                        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #333;
            margin-top: 1.5em;
            margin-bottom: 0.5em;
        }}
        p {{
            margin-bottom: 1em;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 1em;
        }}
        td, th {{
            border: 1px solid #ddd;
            padding: 8px;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
                    }
                    catch (InvalidDataException)
                    {
                        _logger.LogError("Invalid DOCX file structure");
                        throw new InvalidDataException("The document appears to be corrupted or is not a valid Word document");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Word document to HTML");
                throw;
            }
        }

        private string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private async Task<bool> HasDocumentAccess(int documentId, int userId)
        {
            var document = await _dbContext.Documents
                .Include(d => d.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return false;

            // Check if user has any permission level for this document
            return document.Permissions.Any(p => p.UserId == userId);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("User ID claim not found"));
        }

        private bool ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        private string GetUniqueFileName(string fileName)
        {
            return $"{Guid.NewGuid()}_{fileName}";
        }

        private async Task<string> SaveFile(IFormFile file, string fileName)
        {
            var uploadsFolder = Path.Combine(_hostEnvironment.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("uploads", fileName);
        }

        private (long? rangeStart, long? rangeEnd) GetRangeFromRequest(long fileSize)
        {
            var rangeHeader = Request.Headers["Range"].ToString();
            if (string.IsNullOrEmpty(rangeHeader)) return (null, null);

            var match = Regex.Match(rangeHeader, @"bytes=(\d*)-(\d*)");
            if (!match.Success) return (null, null);

            var rangeStart = match.Groups[1].Value;
            var rangeEnd = match.Groups[2].Value;

            long? start = !string.IsNullOrEmpty(rangeStart) ? long.Parse(rangeStart) : null;
            long? end = !string.IsNullOrEmpty(rangeEnd) ? long.Parse(rangeEnd) : fileSize - 1;

            return (start, end);
        }

        private async Task CopyStreamByChunks(Stream source, Stream destination, long bytesToCopy, int bufferSize = 81920)
        {
            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;

            while (totalBytesRead < bytesToCopy)
            {
                var bytesRemaining = bytesToCopy - totalBytesRead;
                var bytesToRead = Math.Min(bufferSize, bytesRemaining);
                var bytesRead = await source.ReadAsync(buffer.AsMemory(0, (int)bytesToRead));

                if (bytesRead == 0) break;

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;
            }
        }

        public class FileCallbackResult : IActionResult
        {
            private readonly string _contentType;
            private readonly Func<Stream, ActionContext, Task> _callback;

            public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
            {
                _contentType = contentType;
                _callback = callback;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                var response = context.HttpContext.Response;
                response.ContentType = _contentType;

                await _callback(response.Body, context);
            }
        }
    }
}
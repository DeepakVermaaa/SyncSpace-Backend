using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.DTO;
using SyncSpaceBackend.Interfaces;
using SyncSpaceBackend.Models;
using System.Security.Claims;
using WebAPI.Context;

namespace SyncSpaceBackend.Controllers
{
    // Controllers/ChatController.cs
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        private readonly IMapper _mapper;

        public ChatController(
            AppDbContext context,
            IChatService chatService,
            ILogger<ChatController> logger,
            IMapper mapper)
        {
            _context = context;
            _chatService = chatService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<IEnumerable<ChatRoom>>> GetUserChatRooms([FromQuery] int? projectId = null)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            try
            {
                var rooms = await _chatService.GetUserChatRoomsAsync(userId, projectId);
                var roomDtos = _mapper.Map<IEnumerable<ChatRoomDto>>(rooms);
                return Ok(roomDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat rooms for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving chat rooms");
            }
        }

        [HttpGet("{projectGroupId}/history")]
        public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetMessageHistory(
            int projectGroupId,
            [FromQuery] int? limit)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            var hasAccess = await _chatService.ValidateUserProjectAccessAsync(userId, projectGroupId);
            if (!hasAccess)
            {
                return Forbid();
            }

            try
            {
                var messages = await _chatService.GetMessageHistoryAsync(projectGroupId, limit);
                var messageDtos = _mapper.Map<IEnumerable<ChatMessageDto>>(messages);
                return Ok(messageDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history for project {ProjectGroupId}", projectGroupId);
                return StatusCode(500, "An error occurred while retrieving chat history");
            }
        }

        [HttpGet("projects")]
        public async Task<ActionResult<IEnumerable<ProjectDropdownDto>>> GetUserProjects()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            try
            {
                var projects = await _context.ProjectGroups
                    .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && p.IsActive)
                    .Select(p => new ProjectDropdownDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving projects");
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            try
            {
                var userId = Convert.ToInt32(userIdString);
                await _chatService.DeleteMessageAsync(messageId, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return StatusCode(500, "An error occurred while deleting the message");
            }
        }

        [HttpPost("rooms")]
        public async Task<ActionResult<ChatRoomDto>> CreateChatRoom([FromBody] CreateChatRoomDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            try
            {
                // Check if user has permission to create chat room
                var hasPermission = await _chatService.ValidateUserCanCreateChatRoomAsync(userId, dto.ProjectGroupId);
                if (!hasPermission)
                {
                    return Forbid();
                }

                var chatRoom = await _chatService.CreateChatRoomAsync(dto.ProjectGroupId, dto.Name);
                var chatRoomDto = _mapper.Map<ChatRoomDto>(chatRoom);
                return Ok(chatRoomDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat room for project {ProjectGroupId}", dto.ProjectGroupId);
                return StatusCode(500, "An error occurred while creating the chat room");
            }
        }
    }
}

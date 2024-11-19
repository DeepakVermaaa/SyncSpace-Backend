using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncSpaceBackend.DTO;
using SyncSpaceBackend.Interfaces;
using SyncSpaceBackend.Models;
using System.Security.Claims;

namespace SyncSpaceBackend.Controllers
{
    // Controllers/ChatController.cs
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        private readonly IMapper _mapper;

        public ChatController(
            IChatService chatService,
            ILogger<ChatController> logger,
            IMapper mapper)
        {
            _chatService = chatService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<IEnumerable<ChatRoom>>> GetUserChatRooms()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            try
            {
                var rooms = await _chatService.GetUserChatRoomsAsync(userId);
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
    }
}

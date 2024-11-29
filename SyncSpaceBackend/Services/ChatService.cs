using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Interfaces;
using SyncSpaceBackend.Models;
using WebAPI.Context;
using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Services
{
    /// <summary>
    /// Provides methods for managing chat messages and chat rooms in a project.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatService"/> class.
        /// </summary>
        /// <param name="context">The database context used to access chat data.</param>
        /// <param name="logger">The logger for logging events and errors.</param>
        /// <param name="mapper">The mapper for object mapping operations.</param>
        public ChatService(
            AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves the message history for a specific project group.
        /// </summary>
        /// <param name="projectGroupId">The ID of the project group for which to retrieve messages.</param>
        /// <param name="limit">The maximum number of messages to retrieve. Defaults to 50 if not specified.</param>
        /// <returns>The task result contains a collection of chat messages.</returns>
        public async Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(int projectGroupId, int? limit = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.ProjectGroupId == projectGroupId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit ?? 50)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Saves a new chat message to the database.
        /// </summary>
        /// <param name="message">The chat message to be saved.</param>
        /// <returns>The task result contains the saved chat message.</returns>
        public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        /// <summary>
        /// Validates whether a user has access to a specific project group.
        /// </summary>
        /// <param name="userId">The ID of the user whose access is to be validated.</param>
        /// <param name="projectGroupId">The ID of the project group to check access for.</param>
        /// <returns>The task result indicates whether the user has access (true) or not (false).</returns>
        public async Task<bool> ValidateUserProjectAccessAsync(int userId, int projectGroupId)
        {

            return await _context.ProjectMembers
                .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectGroupId);
        }

        /// <summary>
        /// Deletes a chat message if the user is authorized to do so.
        /// </summary>
        /// <param name="messageId">The ID of the message to be deleted.</param>
        /// <param name="userId">The ID of the user requesting the deletion.</param>
        /// <returns></returns>
        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message != null)
            {
                message.IsDeleted = true; // Soft delete only
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves the chat rooms associated with a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to retrieve chat rooms.</param>
        /// <returns>The task result contains a collection of chat rooms.</returns>
        public async Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(int userId)
        {
            return await _context.ChatRooms
                .Where(cr => cr.ProjectGroup.ProjectMembers
                    .Any(pm => pm.UserId == userId))
                .Include(cr => cr.ProjectGroup)
                .ToListAsync();
        }

        public async Task<bool> ValidateUserCanCreateChatRoomAsync(int userId, int projectGroupId)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectGroupId && m.UserId == userId);

            return member?.Role == ProjectRole.Admin || member?.Role == ProjectRole.Manager;
        }

        public async Task<ChatRoom> CreateChatRoomAsync(int projectGroupId, string name)
        {
            var chatRoom = new ChatRoom
            {
                ProjectGroupId = projectGroupId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            return chatRoom;
        }
    }

}

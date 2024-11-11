using SyncSpaceBackend.Models;

namespace SyncSpaceBackend.Interfaces
{
    /// <summary>
    /// Provides methods for managing chat messages and chat rooms.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Retrieves the message history for a specific project group.
        /// </summary>
        /// <param name="projectGroupId">The ID of the project group for which to retrieve messages.</param>
        /// <param name="limit">The maximum number of messages to retrieve. Defaults to 50 if not specified.</param>
        /// <returns>The task result contains a collection of chat messages.</returns>
        Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(int projectGroupId, int? limit = 50);

        /// <summary>
        /// Saves a new chat message.
        /// </summary>
        /// <param name="message">The chat message to be saved.</param>
        /// <returns>The task result contains the saved chat message.</returns>
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);

        /// <summary>
        /// Validates whether a user has access to a specific project group.
        /// </summary>
        /// <param name="userId">The ID of the user whose access is to be validated.</param>
        /// <param name="projectGroupId">The ID of the project group to check access for.</param>
        /// <returns>The task result indicates whether the user has access (true) or not (false).</returns>
        Task<bool> ValidateUserProjectAccessAsync(string userId, int projectGroupId);

        /// <summary>
        /// Deletes a chat message if the user is authorized to do so.
        /// </summary>
        /// <param name="messageId">The ID of the message to be deleted.</param>
        /// <param name="userId">The ID of the user requesting the deletion.</param>
        /// <returns></returns>
        Task DeleteMessageAsync(int messageId, int userId);

        /// <summary>
        /// Retrieves the chat rooms associated with a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to retrieve chat rooms.</param>
        /// <returns>The task result contains a collection of chat rooms.</returns>
        Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(string userId);
    }
}

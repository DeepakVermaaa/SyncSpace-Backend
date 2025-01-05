using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.Interfaces
{
    /// <summary>
    /// Defines notification service methods.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets user notifications with optional pagination.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Notifications per page (default is 20).</param>
        /// <returns>List of notifications.</returns>
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Creates a new notification.
        /// </summary>
        /// <param name="notification">Notification to create.</param>
        /// <returns>The created notification.</returns>
        Task<Notification> CreateNotificationAsync(Notification notification);

        /// <summary>
        /// Marks a notification as read for a user.
        /// </summary>
        /// <param name="notificationId">Notification identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Marks all notifications as read for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>The number of notifications marked as read.</returns>
        Task<int> MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Gets the count of unread notifications for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>The count of unread notifications.</returns>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Deletes a notification for a user.
        /// </summary>
        /// <param name="notificationId">Notification identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteNotificationAsync(int notificationId, int userId);

        /// <summary>
        /// Sends a notification to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="message">Notification message.</param>
        /// <param name="type">Type of notification.</param>
        /// <param name="referenceId">Optional reference ID.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> SendNotificationAsync(int userId, string message, NotificationType type, string referenceId = null);
    }
}

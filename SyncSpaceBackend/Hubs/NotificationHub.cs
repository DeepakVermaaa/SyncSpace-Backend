using Microsoft.AspNetCore.SignalR;
using SyncSpaceBackend.Interfaces;
using System.Security.Claims;

namespace SyncSpaceBackend.Hubs
{
    /// <summary>
    /// Represents a SignalR hub for sending notifications to connected clients.
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationHub"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service used to handle notifications.</param>
        /// <param name="logger">The logger for logging events and errors.</param>
        public NotificationHub(
            INotificationService notificationService,
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Called when a connection is established with the hub.
        /// Adds the user to a group based on their user ID for notifications.
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                    _logger.LogInformation($"User {userId} connected to NotificationHub");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a connection is disconnected from the hub.
        /// Removes the user from their group for notifications.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                    _logger.LogInformation($"User {userId} disconnected from NotificationHub");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

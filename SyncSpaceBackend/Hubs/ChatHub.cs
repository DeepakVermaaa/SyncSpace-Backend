// Hubs/ChatHub.cs
using Microsoft.AspNetCore.SignalR;
using SyncSpaceBackend.Interfaces;
using SyncSpaceBackend.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SyncSpaceBackend.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            _logger.LogInformation($"User connected: {user?.Identity?.Name}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = Context.User;
            _logger.LogInformation($"User disconnected: {user?.Identity?.Name}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(int projectGroupId)
        {
            var userIdString = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);
            if (await _chatService.ValidateUserProjectAccessAsync(userId, projectGroupId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, projectGroupId.ToString());
                await Clients.Group(projectGroupId.ToString())
                    .SendAsync("UserJoined", Context.User.FindFirst(ClaimTypes.Name)?.Value);
            }
        }

        public async Task LeaveGroup(int projectGroupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectGroupId.ToString());
            await Clients.Group(projectGroupId.ToString())
                .SendAsync("UserLeft", Context.User.FindFirst(ClaimTypes.Name)?.Value);
        }

        public async Task SendMessage(int projectGroupId, string message)
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Convert.ToInt32(userIdString);

            if (await _chatService.ValidateUserProjectAccessAsync(userId, projectGroupId))
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = userId,
                    SenderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value,
                    Content = message,
                    ProjectGroupId = projectGroupId,
                    Timestamp = DateTime.Now
                };

                try
                {
                    await _chatService.SaveMessageAsync(chatMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    
                }
                try
                {
                    await Clients.Group(projectGroupId.ToString())
                                .SendAsync("ReceiveMessage", chatMessage);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex);
                }
            }
        }
    }
}
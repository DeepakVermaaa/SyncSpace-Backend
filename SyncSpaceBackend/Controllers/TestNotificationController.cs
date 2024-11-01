using Microsoft.AspNetCore.Mvc;
using SyncSpaceBackend.Interfaces;

namespace SyncSpaceBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public TestNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("test/{userId}")]
        public async Task<IActionResult> SendTestNotification(int userId)
        {
            var success = await _notificationService.SendNotificationAsync(
                userId,
                "This is a test notification",
                NotificationType.System
            );

            if (success)
                return Ok("Test notification sent successfully");
            else
                return BadRequest("Failed to send test notification");
        }
    }
}

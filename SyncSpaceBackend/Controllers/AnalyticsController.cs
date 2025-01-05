using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static SyncSpaceBackend.Enums.Enum;
using System.Security.Claims;
using WebAPI.Context;
using Microsoft.EntityFrameworkCore;

namespace SyncSpaceBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            AppDbContext context,
            ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewAnalytics()
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizationId = await GetUserOrganizationId(userId);

                // Get total projects
                var totalProjects = await _context.ProjectGroups
                    .CountAsync(p => p.OrganizationId == organizationId && p.IsActive);

                // Get total tasks
                var totalTasks = await _context.ProjectTasks
                    .CountAsync(t => t.Project.OrganizationId == organizationId);

                // Get project completion rate
                var completedProjects = await _context.ProjectGroups
                    .CountAsync(p => p.OrganizationId == organizationId &&
                                      p.IsActive &&
                                      p.Status == ProjectStatus.Completed);

                // Get task completion rate
                var completedTasks = await _context.ProjectTasks
                    .CountAsync(t => t.Project.OrganizationId == organizationId &&
                                      t.Status == TaskStatusEnum.Completed);

                return Ok(new
                {
                    TotalProjects = totalProjects,
                    TotalTasks = totalTasks,
                    CompletedProjects = completedProjects,
                    CompletedTasks = completedTasks,
                    ProjectCompletionRate = totalProjects > 0
                        ? (double)completedProjects / totalProjects * 100
                        : 0,
                    TaskCompletionRate = totalTasks > 0
                        ? (double)completedTasks / totalTasks * 100
                        : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overview analytics");
                return StatusCode(500, new { Message = "An error occurred while retrieving analytics" });
            }
        }

        [HttpGet("tasks-distribution")]
        public async Task<IActionResult> GetTasksDistribution()
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizationId = await GetUserOrganizationId(userId);

                // Get task distribution by status
                var tasksDistribution = await _context.ProjectTasks
                    .Where(t => t.Project.OrganizationId == organizationId)
                    .GroupBy(t => t.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get tasks by priority
                var tasksByPriority = await _context.ProjectTasks
                    .Where(t => t.Project.OrganizationId == organizationId)
                    .GroupBy(t => t.Priority)
                    .Select(g => new
                    {
                        Priority = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TasksByStatus = tasksDistribution,
                    TasksByPriority = tasksByPriority
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks distribution");
                return StatusCode(500, new { Message = "An error occurred while retrieving tasks distribution" });
            }
        }

        [HttpGet("team-performance")]
        public async Task<IActionResult> GetTeamPerformance()
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizationId = await GetUserOrganizationId(userId);

                // Get team performance metrics
                var teamPerformance = await _context.Users
                    .Where(u => u.OrganizationId == organizationId)
                    .Select(u => new
                    {
                        UserId = u.Id,
                        UserName = $"{u.FirstName} {u.LastName}",
                        TotalTasks = _context.ProjectTasks.Count(t => t.AssignedToId == u.Id),
                        CompletedTasks = _context.ProjectTasks.Count(t =>
                            t.AssignedToId == u.Id &&
                            t.Status == TaskStatusEnum.Completed)
                    })
                    .ToListAsync();

                return Ok(teamPerformance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team performance");
                return StatusCode(500, new { Message = "An error occurred while retrieving team performance" });
            }
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        private async Task<int> GetUserOrganizationId(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.OrganizationId ?? 0;
        }
    }
}

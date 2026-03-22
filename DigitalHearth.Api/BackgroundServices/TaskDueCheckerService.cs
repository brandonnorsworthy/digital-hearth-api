using DigitalHearth.Api.Data;
using DigitalHearth.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.BackgroundServices;

public class TaskDueCheckerService(IServiceScopeFactory scopeFactory, ILogger<TaskDueCheckerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                var now = DateTime.UtcNow;
                var windowStart = now.AddHours(-1);

                // Filter in SQL: only fetch tasks whose due date falls in the last hour window
                var tasks = await db.RecurringTasks
                    .Where(t =>
                        (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays) >= windowStart &&
                        (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays) < now)
                    .Include(t => t.NotifPreferences)
                    .Include(t => t.Household).ThenInclude(h => h.Members)
                    .ToListAsync(stoppingToken);

                foreach (var task in tasks)
                {
                    var optedOutUserIds = task.NotifPreferences
                        .Select(p => p.UserId)
                        .ToHashSet();

                    foreach (var member in task.Household.Members)
                    {
                        if (optedOutUserIds.Contains(member.Id)) continue;

                        await push.SendToUserAsync(
                            member.Id,
                            $"Task Due: {task.Name}",
                            $"{task.Name} is now due.",
                            stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TaskDueCheckerService");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

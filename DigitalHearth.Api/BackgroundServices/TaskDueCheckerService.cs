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
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                var now = DateTime.UtcNow;
                var windowStart = now.AddHours(-1);

                var tasks = await db.RecurringTasks
                    .Include(t => t.NotifPreferences)
                    .Include(t => t.Household).ThenInclude(h => h.Members)
                    .ToListAsync(stoppingToken);

                foreach (var task in tasks)
                {
                    var nextDueAt = (task.LastCompletedAt ?? task.CreatedAt).AddDays(task.IntervalDays);

                    // Notify only when the task first became overdue within the last hour window
                    if (nextDueAt < windowStart || nextDueAt >= now) continue;

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
        }
    }
}

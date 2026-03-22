using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;

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
                var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                var now = DateTime.UtcNow;
                var windowStart = now.AddHours(-1);

                var tasks = await taskRepo.GetDueInWindowAsync(windowStart, now, stoppingToken);

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

using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;

namespace DigitalHearth.Api.BackgroundServices;

public class TaskDueCheckerService(IServiceScopeFactory scopeFactory, ILogger<TaskDueCheckerService> logger)
    : BackgroundService
{
    private DateTime _lastCleanup = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                var notifRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                var now = DateTime.UtcNow;
                var tasks = await taskRepo.GetDueTasksAsync(now, stoppingToken);

                foreach (var task in tasks)
                {
                    var dueAt = (task.LastCompletedAt ?? task.CreatedAt).AddDays(task.IntervalDays);
                    var optedOutUserIds = task.NotifPreferences.Select(p => p.UserId).ToHashSet();

                    foreach (var member in task.Household.Members)
                    {
                        if (optedOutUserIds.Contains(member.Id)) continue;

                        foreach (var sub in member.PushSubscriptions)
                        {
                            if (await notifRepo.HasLogAsync(sub.Id, task.Id, dueAt, stoppingToken))
                                continue;

                            var success = await push.SendToSubscriptionAsync(
                                sub,
                                $"Task Due: {task.Name}",
                                $"{task.Name} is now due.",
                                stoppingToken);

                            await notifRepo.AddLogAsync(new NotificationLog
                            {
                                PushSubscriptionId = sub.Id,
                                RecurringTaskId = task.Id,
                                DueAt = dueAt,
                                SentAt = now,
                                Status = success ? NotificationStatus.Sent : NotificationStatus.Failed,
                                ErrorMessage = success ? null : "Send failed — see application logs for details"
                            }, stoppingToken);

                            if (success)
                                await notifRepo.UpdateLastSuccessfulPushAsync(sub.Id, now, stoppingToken);
                        }
                    }
                }

                if (now - _lastCleanup >= TimeSpan.FromDays(1))
                {
                    var cutoff = now.AddMonths(-1);
                    await notifRepo.DeleteStaleSubscriptionsAsync(cutoff, stoppingToken);
                    _lastCleanup = now;
                    logger.LogInformation("Pruned stale push subscriptions older than {Cutoff:O}", cutoff);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TaskDueCheckerService");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

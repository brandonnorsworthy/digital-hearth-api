namespace DigitalHearth.Api.DTOs.Notification;

public record UserNotifSettingsResponse(
    int? TaskReminderHour,
    int? MediumTermDaysAhead,
    bool MealPlannerNotifs,
    bool ShortTermTaskNotifs,
    bool MediumTermTaskNotifs,
    bool LongTermTaskNotifs,
    bool TaskCompletedNotifs
);

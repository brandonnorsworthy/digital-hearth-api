using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class NotificationController(
    ICurrentUserService currentUser,
    INotificationService notificationService,
    IPushNotificationService push,
    IConfiguration config) : ApiControllerBase
{
    [HttpGet("api/notifications/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = config["Vapid:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
            return StatusCode(503, new { error = "VAPID keys not configured" });
        return Ok(new { publicKey });
    }

    [HttpPost("api/notifications/subscription")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        await notificationService.SubscribeAsync(req, user!, ct);
        return StatusCode(201);
    }

    [HttpDelete("api/notifications/subscription")]
    public async Task<IActionResult> Unsubscribe(CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await notificationService.UnsubscribeAsync(user!, ct));
    }

    [HttpGet("api/households/{householdId:int}/notifications/preferences")]
    public async Task<IActionResult> GetPreferences(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await notificationService.GetPreferencesAsync(householdId, user!, ct));
    }

    [HttpPost("api/notifications/preferences/opt-out")]
    public async Task<IActionResult> OptOut([FromBody] OptOutRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        await notificationService.OptOutAsync(req, user!, ct);
        return StatusCode(201);
    }

    [HttpDelete("api/notifications/preferences/opt-out/{taskId:int}")]
    public async Task<IActionResult> RemoveOptOut(int taskId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await notificationService.RemoveOptOutAsync(taskId, user!, ct));
    }

    [HttpGet("api/notifications/settings")]
    public async Task<IActionResult> GetUserNotifSettings(CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await notificationService.GetUserNotifSettingsAsync(user!, ct));
    }

    [HttpPut("api/notifications/settings")]
    public async Task<IActionResult> UpdateUserNotifSettings(
        [FromBody] UpdateUserNotifSettingsRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await notificationService.UpdateUserNotifSettingsAsync(req, user!, ct));
    }

#if DEBUG
    [HttpPost("api/notifications/test/{userId:int}")]
    public async Task<IActionResult> Test(int userId, CancellationToken ct)
    {
        var (_, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        await push.SendToUserAsync(userId, "Test Notification", "Push is working!", ct);
        return Ok(new { sent = true });
    }
#endif
}

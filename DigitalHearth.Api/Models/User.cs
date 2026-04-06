namespace DigitalHearth.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = "member";
    public Guid HouseholdId { get; set; }
    public bool IsActive { get; set; } = true;

    public Household Household { get; set; } = null!;
    public ICollection<TaskCompletion> Completions { get; set; } = [];
    public ICollection<PushSubscription> PushSubscriptions { get; set; } = [];
    public ICollection<NotifPreference> NotifPreferences { get; set; } = [];
    public ICollection<MealLibrary> CreatedMeals { get; set; } = [];
    public UserNotifSettings? NotifSettings { get; set; }
}

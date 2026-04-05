using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<RecurringTask> RecurringTasks => Set<RecurringTask>();
    public DbSet<TaskCompletion> TaskCompletions => Set<TaskCompletion>();
    public DbSet<WeeklyMeal> WeeklyMeals => Set<WeeklyMeal>();
    public DbSet<MealLibrary> MealLibrary => Set<MealLibrary>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<NotifPreference> NotifPreferences => Set<NotifPreference>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<UserNotifSettings> UserNotifSettings => Set<UserNotifSettings>();
    public DbSet<MealFavorite> MealFavorites => Set<MealFavorite>();
    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Household
        b.Entity<Household>()
            .HasIndex(h => h.JoinCode).IsUnique();

        // User: globally unique username
        b.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        // RecurringTask: nullable FK to last completer
        b.Entity<RecurringTask>()
            .HasOne(t => t.LastCompletedByUser)
            .WithMany()
            .HasForeignKey(t => t.LastCompletedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<RecurringTask>()
            .HasIndex(t => t.HouseholdId);

        // TaskCompletion
        b.Entity<TaskCompletion>()
            .HasIndex(c => c.TaskId);

        // NotifPreference: unique per (userId, taskId)
        b.Entity<NotifPreference>()
            .HasIndex(n => new { n.UserId, n.TaskId }).IsUnique();

        // PushSubscription: unique per (userId, endpoint)
        b.Entity<PushSubscription>()
            .HasIndex(p => new { p.UserId, p.Endpoint }).IsUnique();

        // NotificationLog: unique per subscription + task + due instance; cascade on both FKs
        b.Entity<NotificationLog>()
            .HasIndex(n => new { n.PushSubscriptionId, n.RecurringTaskId, n.DueAt }).IsUnique();

        b.Entity<NotificationLog>()
            .HasOne(n => n.PushSubscription)
            .WithMany()
            .HasForeignKey(n => n.PushSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<NotificationLog>()
            .HasOne(n => n.RecurringTask)
            .WithMany()
            .HasForeignKey(n => n.RecurringTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserNotifSettings: one-to-one with User
        b.Entity<UserNotifSettings>()
            .HasIndex(s => s.UserId).IsUnique();

        // MealFavorite: unique per (userId, mealLibraryId)
        b.Entity<MealFavorite>()
            .HasIndex(f => new { f.UserId, f.MealLibraryId }).IsUnique();

        b.Entity<MealFavorite>()
            .HasOne(f => f.MealLibrary)
            .WithMany()
            .HasForeignKey(f => f.MealLibraryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Image: one-to-one with MealLibrary, cascade delete
        b.Entity<Image>()
            .HasOne(i => i.MealLibrary)
            .WithOne(l => l.Image)
            .HasForeignKey<Image>(i => i.MealLibraryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Image>()
            .HasIndex(i => i.MealLibraryId).IsUnique();

        // WeeklyMeal
        b.Entity<WeeklyMeal>()
            .HasIndex(m => new { m.HouseholdId, m.WeekOf });

        // When a library meal is deleted, weekly meals keep their name (SetNull on FK)
        b.Entity<WeeklyMeal>()
            .HasOne(m => m.MealLibrary)
            .WithMany(l => l.WeeklyMeals)
            .HasForeignKey(m => m.MealLibraryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

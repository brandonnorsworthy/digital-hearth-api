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

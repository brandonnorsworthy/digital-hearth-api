namespace DigitalHearth.Api.Models;

public class MealFavorite
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MealLibraryId { get; set; }

    public User User { get; set; } = null!;
    public MealLibrary MealLibrary { get; set; } = null!;
}

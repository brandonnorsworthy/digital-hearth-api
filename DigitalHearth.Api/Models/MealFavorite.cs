namespace DigitalHearth.Api.Models;

public class MealFavorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MealLibraryId { get; set; }

    public User User { get; set; } = null!;
    public MealLibrary MealLibrary { get; set; } = null!;
}

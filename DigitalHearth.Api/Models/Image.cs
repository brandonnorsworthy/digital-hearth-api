namespace DigitalHearth.Api.Models;

public class Image
{
    public int Id { get; set; }
    public int MealLibraryId { get; set; }
    public Guid ImageGuid { get; set; }
    public string ImageData { get; set; } = null!;
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MealLibrary MealLibrary { get; set; } = null!;
}

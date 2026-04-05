namespace DigitalHearth.Api.DTOs.Household;

public record CreateHouseholdRequest(
    string HouseholdName,
    string Username,
    string Password,
    string? WeekResetDay);

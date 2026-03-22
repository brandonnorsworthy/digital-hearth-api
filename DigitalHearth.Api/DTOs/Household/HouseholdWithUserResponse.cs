using DigitalHearth.Api.DTOs.Auth;

namespace DigitalHearth.Api.DTOs.Household;

public record HouseholdWithUserResponse(MeResponse User, HouseholdResponse Household);

namespace DigitalHearth.Api.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

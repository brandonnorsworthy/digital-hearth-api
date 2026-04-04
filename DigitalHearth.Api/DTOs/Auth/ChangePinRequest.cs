namespace DigitalHearth.Api.DTOs.Auth;

public record ChangePinRequest(string CurrentPin, string NewPin);

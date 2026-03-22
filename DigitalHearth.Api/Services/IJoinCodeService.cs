using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public interface IJoinCodeService
{
    Task<string> GenerateUniqueCodeAsync(IHouseholdRepository households, CancellationToken ct = default);
}

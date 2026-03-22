using DigitalHearth.Api.Data;

namespace DigitalHearth.Api.Services;

public interface IJoinCodeService
{
    Task<string> GenerateUniqueCodeAsync(AppDbContext db, CancellationToken ct = default);
}

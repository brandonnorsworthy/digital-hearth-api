using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(int id, CancellationToken ct);
    Task<Household?> GetByJoinCodeAsync(string joinCode, CancellationToken ct);
    Task<Household> CreateAsync(Household household, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

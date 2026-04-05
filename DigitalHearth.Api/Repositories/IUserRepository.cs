using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
    Task<User> CreateAsync(User user, CancellationToken ct);
    Task<List<MemberResponse>> GetMembersByHouseholdAsync(Guid householdId, CancellationToken ct);
    Task UpdatePinHashAsync(Guid userId, string pinHash, CancellationToken ct);
}

using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
    Task<User> CreateAsync(User user, CancellationToken ct);
    Task<List<MemberResponse>> GetMembersByHouseholdAsync(int householdId, CancellationToken ct);
}

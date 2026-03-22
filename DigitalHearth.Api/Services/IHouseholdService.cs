using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface IHouseholdService
{
    Task<ServiceResult<HouseholdWithUserResponse>> CreateAsync(CreateHouseholdRequest req, CancellationToken ct = default);
    Task<ServiceResult<HouseholdWithUserResponse>> JoinAsync(JoinHouseholdRequest req, CancellationToken ct = default);
    Task<ServiceResult<HouseholdResponse>> GetByIdAsync(int id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<MemberResponse>>> GetMembersAsync(int id, User user, CancellationToken ct = default);
    Task<ServiceResult<HouseholdResponse>> UpdateAsync(int id, UpdateHouseholdRequest req, User user, CancellationToken ct = default);
}

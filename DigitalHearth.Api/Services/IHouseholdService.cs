using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface IHouseholdService
{
    Task<ServiceResult<HouseholdWithUserResponse>> CreateAsync(CreateHouseholdRequest req, CancellationToken ct = default);
    Task<ServiceResult<HouseholdWithUserResponse>> JoinAsync(JoinHouseholdRequest req, CancellationToken ct = default);
    Task<ServiceResult<HouseholdResponse>> GetByIdAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<MemberResponse>>> GetMembersAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<HouseholdResponse>> RegenerateJoinCodeAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<HouseholdResponse>> UpdateAsync(Guid id, UpdateHouseholdRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult<bool>> KickMemberAsync(Guid householdId, Guid memberId, User requester, CancellationToken ct = default);
}

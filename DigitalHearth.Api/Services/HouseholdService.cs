using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class HouseholdService(IHouseholdRepository households, IUserRepository users, ICurrentUserService currentUser, IJoinCodeService joinCodeService) : IHouseholdService
{
    private static readonly string[] ValidDays =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    private static int DayNameToInt(string name) =>
        name.ToLowerInvariant() switch
        {
            "sunday" => 0, "monday" => 1, "tuesday" => 2, "wednesday" => 3,
            "thursday" => 4, "friday" => 5, "saturday" => 6, _ => -1
        };

    private static string DayIntToName(int day) =>
        day switch
        {
            0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday",
            4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "Monday"
        };

    private static HouseholdResponse ToResponse(Household h) =>
        new(h.Id, h.Name, h.JoinCode, DayIntToName(h.WeekResetDay));

    public async Task<ServiceResult<HouseholdWithUserResponse>> CreateAsync(CreateHouseholdRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.HouseholdName) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Pin))
            return ServiceResult<HouseholdWithUserResponse>.BadRequest("HouseholdName, Username, and Pin are required");

        var weekResetDay = DayNameToInt(req.WeekResetDay ?? "Monday");
        if (weekResetDay < 0)
            return ServiceResult<HouseholdWithUserResponse>.BadRequest("WeekResetDay must be a valid day name (e.g. Monday)");

        var normalizedUsername = req.Username.ToLowerInvariant();
        if (await users.UsernameExistsAsync(normalizedUsername, ct))
            return ServiceResult<HouseholdWithUserResponse>.Conflict("Username already taken");

        var joinCode = await joinCodeService.GenerateUniqueCodeAsync(households, ct);

        var household = new Household
        {
            Name = req.HouseholdName,
            JoinCode = joinCode,
            WeekResetDay = weekResetDay
        };
        await households.CreateAsync(household, ct);

        var user = new User
        {
            Username = normalizedUsername,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            Role = "admin",
            HouseholdId = household.Id
        };
        await users.CreateAsync(user, ct);

        currentUser.SetUserId(user.Id);

        return ServiceResult<HouseholdWithUserResponse>.Ok(new HouseholdWithUserResponse(
            new MeResponse(user.Id, user.Username, user.HouseholdId),
            ToResponse(household)));
    }

    public async Task<ServiceResult<HouseholdWithUserResponse>> JoinAsync(JoinHouseholdRequest req, CancellationToken ct = default)
    {
        var household = await households.GetByJoinCodeAsync(req.JoinCode, ct);

        if (household is null)
            return ServiceResult<HouseholdWithUserResponse>.NotFound("Join code not found");

        var normalizedUsername = req.Username.ToLowerInvariant();
        if (await users.UsernameExistsAsync(normalizedUsername, ct))
            return ServiceResult<HouseholdWithUserResponse>.Conflict("Username already taken");

        var user = new User
        {
            Username = normalizedUsername,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            Role = "member",
            HouseholdId = household.Id
        };
        await users.CreateAsync(user, ct);

        currentUser.SetUserId(user.Id);

        return ServiceResult<HouseholdWithUserResponse>.Ok(new HouseholdWithUserResponse(
            new MeResponse(user.Id, user.Username, user.HouseholdId),
            ToResponse(household)));
    }

    public async Task<ServiceResult<HouseholdResponse>> GetByIdAsync(int id, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != id)
            return ServiceResult<HouseholdResponse>.Forbidden();

        var household = await households.GetByIdAsync(id, ct);
        if (household is null)
            return ServiceResult<HouseholdResponse>.NotFound("Household not found");

        return ServiceResult<HouseholdResponse>.Ok(ToResponse(household));
    }

    public async Task<ServiceResult<IReadOnlyList<MemberResponse>>> GetMembersAsync(int id, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != id)
            return ServiceResult<IReadOnlyList<MemberResponse>>.Forbidden();

        var members = await users.GetMembersByHouseholdAsync(id, ct);

        return ServiceResult<IReadOnlyList<MemberResponse>>.Ok(members);
    }

    public async Task<ServiceResult<HouseholdResponse>> UpdateAsync(int id, UpdateHouseholdRequest req, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != id)
            return ServiceResult<HouseholdResponse>.Forbidden();

        if (user.Role != "admin")
            return ServiceResult<HouseholdResponse>.Forbidden();

        var household = await households.GetByIdAsync(id, ct);
        if (household is null)
            return ServiceResult<HouseholdResponse>.NotFound("Household not found");

        if (req.Name is not null) household.Name = req.Name;
        if (req.WeekResetDay is not null)
        {
            var day = DayNameToInt(req.WeekResetDay);
            if (day < 0)
                return ServiceResult<HouseholdResponse>.BadRequest("WeekResetDay must be a valid day name (e.g. Monday)");
            household.WeekResetDay = day;
        }

        await households.SaveAsync(ct);
        return ServiceResult<HouseholdResponse>.Ok(ToResponse(household));
    }
}

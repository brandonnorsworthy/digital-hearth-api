using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[Route("api/households")]
public class HouseholdController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IJoinCodeService joinCodeService) : ApiControllerBase
{
    private static readonly string[] ValidDays =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    private static int DayNameToInt(string? name) =>
        (name ?? "Monday").ToLowerInvariant() switch
        {
            "sunday" => 0, "monday" => 1, "tuesday" => 2, "wednesday" => 3,
            "thursday" => 4, "friday" => 5, "saturday" => 6, _ => 1
        };

    private static string DayIntToName(int day) =>
        day switch
        {
            0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday",
            4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "Monday"
        };

    private HouseholdResponse ToResponse(Household h) =>
        new(h.Id, h.Name, h.JoinCode, DayIntToName(h.WeekResetDay));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHouseholdRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.HouseholdName) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Pin))
            return BadRequest(new { error = "HouseholdName, Username, and Pin are required" });

        if (await db.Users.AnyAsync(u => u.Username.ToLower() == req.Username.ToLower(), ct))
            return Conflict(new { error = "Username already taken" });

        var joinCode = await joinCodeService.GenerateUniqueCodeAsync(db, ct);

        var household = new Household
        {
            Name = req.HouseholdName,
            JoinCode = joinCode,
            WeekResetDay = DayNameToInt(req.WeekResetDay)
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);

        var user = new User
        {
            Username = req.Username,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            Role = "admin",
            HouseholdId = household.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        ((CurrentUserService)currentUser).SetUserId(user.Id);

        return CreatedAtAction(nameof(GetById), new { id = household.Id }, new
        {
            user = new MeResponse(user.Id, user.Username, user.HouseholdId),
            household = ToResponse(household)
        });
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinHouseholdRequest req, CancellationToken ct)
    {
        var household = await db.Households
            .FirstOrDefaultAsync(h => h.JoinCode.ToUpper() == req.JoinCode.ToUpper(), ct);

        if (household is null)
            return NotFound(new { error = "Join code not found" });

        if (await db.Users.AnyAsync(u => u.Username.ToLower() == req.Username.ToLower(), ct))
            return Conflict(new { error = "Username already taken" });

        var user = new User
        {
            Username = req.Username,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            Role = "member",
            HouseholdId = household.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        ((CurrentUserService)currentUser).SetUserId(user.Id);

        return Ok(new
        {
            user = new MeResponse(user.Id, user.Username, user.HouseholdId),
            household = ToResponse(household)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != id)
            return Forbid();

        var household = await db.Households.FindAsync([id], ct);
        if (household is null) return NotFound(new { error = "Household not found" });

        return Ok(ToResponse(household));
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != id)
            return Forbid();

        var members = await db.Users
            .Where(u => u.HouseholdId == id)
            .Select(u => new MemberResponse(u.Id, u.Username, u.Role))
            .ToListAsync(ct);

        return Ok(members);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHouseholdRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != id)
            return Forbid();

        if (user.Role != "admin")
            return StatusCode(403, new { error = "Admin only" });

        var household = await db.Households.FindAsync([id], ct);
        if (household is null) return NotFound(new { error = "Household not found" });

        if (req.Name is not null) household.Name = req.Name;
        if (req.WeekResetDay is not null) household.WeekResetDay = DayNameToInt(req.WeekResetDay);

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(household));
    }
}

using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[Route("api/households")]
public class HouseholdController(ICurrentUserService currentUser, IHouseholdService householdService) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHouseholdRequest req, CancellationToken ct)
    {
        var result = await householdService.CreateAsync(req, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Household.Id }, result.Value);
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinHouseholdRequest req, CancellationToken ct)
        => ToActionResult(await householdService.JoinAsync(req, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await householdService.GetByIdAsync(id, user!, ct));
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await householdService.GetMembersAsync(id, user!, ct));
    }

    [HttpPost("{id:guid}/regenerate-join-code")]
    public async Task<IActionResult> RegenerateJoinCode(Guid id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await householdService.RegenerateJoinCodeAsync(id, user!, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHouseholdRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await householdService.UpdateAsync(id, req, user!, ct));
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> KickMember(Guid id, Guid memberId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await householdService.KickMemberAsync(id, memberId, user!, ct));
    }
}

using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Users.FindAsync([id], ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), ct);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
    {
        return await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash, CancellationToken ct)
    {
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, passwordHash), ct);
    }

    public async Task<List<MemberResponse>> GetMembersByHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        return await db.Users
            .Where(u => u.HouseholdId == householdId)
            .Select(u => new MemberResponse(u.Id, u.Username, u.Role))
            .ToListAsync(ct);
    }
}

using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class HouseholdRepository(AppDbContext db) : IHouseholdRepository
{
    public async Task<Household?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await db.Households.FindAsync([id], ct);
    }

    public async Task<Household?> GetByJoinCodeAsync(string joinCode, CancellationToken ct)
    {
        return await db.Households
            .FirstOrDefaultAsync(h => h.JoinCode.ToUpper() == joinCode.ToUpper(), ct);
    }

    public async Task<Household> CreateAsync(Household household, CancellationToken ct)
    {
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return household;
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}

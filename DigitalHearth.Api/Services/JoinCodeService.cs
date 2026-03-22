using System.Security.Cryptography;
using DigitalHearth.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class JoinCodeService : IJoinCodeService
{
    private static readonly char[] Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<string> GenerateUniqueCodeAsync(AppDbContext db, CancellationToken ct = default)
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetString(Chars, 6);
        }
        while (await db.Households.AnyAsync(h => h.JoinCode == code, ct));

        return code;
    }
}

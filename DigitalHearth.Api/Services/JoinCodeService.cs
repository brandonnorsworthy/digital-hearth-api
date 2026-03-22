using System.Security.Cryptography;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class JoinCodeService : IJoinCodeService
{
    private static readonly char[] Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<string> GenerateUniqueCodeAsync(IHouseholdRepository households, CancellationToken ct = default)
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetString(Chars, 6);
        }
        while (await households.GetByJoinCodeAsync(code, ct) is not null);

        return code;
    }
}

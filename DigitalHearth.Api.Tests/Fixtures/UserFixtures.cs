using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class UserFixtures
{
    public static readonly Guid DefaultId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultHouseholdId = new("00000000-0000-0000-0000-000000000010");
    public static readonly Guid OutsideHouseholdId = new("00000000-0000-0000-0000-000000000099");

    public static User Member(Guid? id = null, Guid? householdId = null, string username = "alice") => new()
    {
        Id = id ?? DefaultId,
        Username = username,
        PinHash = BCrypt.Net.BCrypt.HashPassword("1234"),
        Role = "member",
        HouseholdId = householdId ?? DefaultHouseholdId
    };

    public static User Admin(Guid? id = null, Guid? householdId = null, string username = "alice") => new()
    {
        Id = id ?? DefaultId,
        Username = username,
        PinHash = BCrypt.Net.BCrypt.HashPassword("1234"),
        Role = "admin",
        HouseholdId = householdId ?? DefaultHouseholdId
    };

    public static User InHousehold(Guid householdId) => Member(householdId: householdId);

    public static User OutsideHousehold(Guid? householdId = null) => Member(householdId: householdId ?? OutsideHouseholdId);
}

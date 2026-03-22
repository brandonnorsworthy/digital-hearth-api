using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class UserFixtures
{
    public static User Member(int id = 1, int householdId = 10, string username = "alice") => new()
    {
        Id = id,
        Username = username,
        PinHash = BCrypt.Net.BCrypt.HashPassword("1234"),
        Role = "member",
        HouseholdId = householdId
    };

    public static User Admin(int id = 1, int householdId = 10, string username = "alice") => new()
    {
        Id = id,
        Username = username,
        PinHash = BCrypt.Net.BCrypt.HashPassword("1234"),
        Role = "admin",
        HouseholdId = householdId
    };

    public static User InHousehold(int householdId) => Member(householdId: householdId);

    public static User OutsideHousehold(int householdId = 99) => Member(householdId: householdId);
}

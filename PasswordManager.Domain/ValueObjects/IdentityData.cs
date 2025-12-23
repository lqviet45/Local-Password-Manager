using System.Text.Json;

namespace PasswordManager.Domain.ValueObjects;

/// <summary>
/// Strongly-typed payload for identity vault items.
/// Serialized to JSON before encryption.
/// </summary>
public sealed record IdentityData
{
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? PassportNumber { get; init; }
    public string? LicenseNumber { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static bool TryFromJson(string json, out IdentityData? data)
    {
        try
        {
            data = JsonSerializer.Deserialize<IdentityData>(json);
            return data is not null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }
}



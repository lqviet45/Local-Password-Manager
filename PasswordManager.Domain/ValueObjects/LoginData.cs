using System.Text.Json;

namespace PasswordManager.Domain.ValueObjects;

/// <summary>
/// Strongly-typed payload for login-type vault items.
/// Serialized to JSON before encryption.
/// </summary>
public sealed record LoginData
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static bool TryFromJson(string json, out LoginData? data)
    {
        try
        {
            data = JsonSerializer.Deserialize<LoginData>(json);
            return data is not null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }
}



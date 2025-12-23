using System.Text.Json;

namespace PasswordManager.Domain.ValueObjects;

/// <summary>
/// Strongly-typed payload for secure note vault items.
/// Serialized to JSON before encryption.
/// </summary>
public sealed record SecureNoteData
{
    public string? Content { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static bool TryFromJson(string json, out SecureNoteData? data)
    {
        try
        {
            data = JsonSerializer.Deserialize<SecureNoteData>(json);
            return data is not null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }
}



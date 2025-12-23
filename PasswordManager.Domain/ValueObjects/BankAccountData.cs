using System.Text.Json;

namespace PasswordManager.Domain.ValueObjects;

/// <summary>
/// Strongly-typed payload for bank account vault items.
/// Serialized to JSON before encryption.
/// </summary>
public sealed record BankAccountData
{
    public string? BankName { get; init; }
    public string? AccountHolderName { get; init; }
    public string? AccountNumber { get; init; }
    public string? RoutingNumber { get; init; }
    public string? IBAN { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static bool TryFromJson(string json, out BankAccountData? data)
    {
        try
        {
            data = JsonSerializer.Deserialize<BankAccountData>(json);
            return data is not null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }
}



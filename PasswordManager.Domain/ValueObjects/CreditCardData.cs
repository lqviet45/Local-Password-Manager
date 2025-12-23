using System.Text.Json;

namespace PasswordManager.Domain.ValueObjects;

/// <summary>
/// Strongly-typed payload for credit card vault items.
/// Serialized to JSON before encryption.
/// </summary>
public sealed record CreditCardData
{
    public string? CardholderName { get; init; }
    public string? CardNumber { get; init; }
    public string? ExpiryMonth { get; init; }
    public string? ExpiryYear { get; init; }
    public string? CVV { get; init; }
    public string? BillingAddress { get; init; }
    public string? ZipCode { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static bool TryFromJson(string json, out CreditCardData? data)
    {
        try
        {
            data = JsonSerializer.Deserialize<CreditCardData>(json);
            return data is not null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }
}



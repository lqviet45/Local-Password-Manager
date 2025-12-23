using PasswordManager.Domain.Enums;

namespace PasswordManager.Shared.Vault.Dto;

/// <summary>
/// Input contract for creating or updating vault items.
/// </summary>
public sealed record VaultItemRequest
{
    public required VaultItemType Type { get; init; }
    public required string Name { get; init; }
    public string? Username { get; init; }
    public required string Password { get; init; }
    public string? Url { get; init; }
    public string? Notes { get; init; }
    public string? Tags { get; init; }
    public bool IsFavorite { get; init; }
}


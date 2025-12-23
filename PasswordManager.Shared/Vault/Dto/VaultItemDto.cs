using PasswordManager.Domain.Enums;

namespace PasswordManager.Shared.Vault.Dto;

/// <summary>
/// Data contract for vault items exposed outside Infrastructure layer.
/// </summary>
public sealed record VaultItemDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required VaultItemType Type { get; init; }
    public required string Name { get; init; }
    public string? Username { get; init; }
    public required string EncryptedData { get; init; }
    public string? Url { get; init; }
    public string? Notes { get; init; }
    public string? Tags { get; init; }
    public bool IsFavorite { get; init; }
    public long Version { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime LastModifiedUtc { get; init; }
    public bool IsDeleted { get; init; }
    public bool NeedsSync { get; init; }
    public string? DataHash { get; init; }
}


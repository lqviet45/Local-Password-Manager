using PasswordManager.Domain.Entities;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Application.Common.Mapping;

internal static class VaultItemMapping
{
    public static VaultItemDto ToDto(this VaultItem item) =>
        new()
        {
            Id = item.Id,
            UserId = item.UserId,
            Type = item.Type,
            Name = item.Name,
            Username = item.Username,
            EncryptedData = item.EncryptedData,
            Url = item.Url,
            Notes = item.Notes,
            Tags = item.Tags,
            IsFavorite = item.IsFavorite,
            Version = item.Version,
            CreatedAtUtc = item.CreatedAtUtc,
            LastModifiedUtc = item.LastModifiedUtc,
            IsDeleted = item.IsDeleted,
            NeedsSync = item.NeedsSync,
            DataHash = item.DataHash
        };
}


using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Shared.Vault.Commands;

/// <summary>
/// Updates an existing vault item.
/// </summary>
public sealed record UpdateVaultItemCommand(
    Guid UserId,
    Guid ItemId,
    VaultItemRequest Item,
    byte[] EncryptionKey) : ICommand<VaultItemDto>;


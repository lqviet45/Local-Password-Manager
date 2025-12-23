using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Shared.Vault.Commands;

/// <summary>
/// Creates a new vault item for the specified user.
/// </summary>
public sealed record CreateVaultItemCommand(
    Guid UserId,
    VaultItemRequest Item,
    byte[] EncryptionKey) : ICommand<VaultItemDto>;


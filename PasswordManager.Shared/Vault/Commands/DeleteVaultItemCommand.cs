using PasswordManager.Shared.Core.Message;

namespace PasswordManager.Shared.Vault.Commands;

/// <summary>
/// Soft delete a vault item.
/// </summary>
public sealed record DeleteVaultItemCommand(Guid ItemId) : ICommand;


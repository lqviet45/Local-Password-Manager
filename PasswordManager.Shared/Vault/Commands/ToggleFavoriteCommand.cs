using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Shared.Vault.Commands;

/// <summary>
/// Toggle favorite flag on a vault item.
/// </summary>
public sealed record ToggleFavoriteCommand(Guid ItemId) : ICommand<VaultItemDto>;


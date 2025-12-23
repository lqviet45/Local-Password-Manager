using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Shared.Vault.Queries;

/// <summary>
/// Retrieves vault items for a user.
/// </summary>
public sealed record GetVaultItemsQuery(Guid UserId, bool IncludeDeleted = false)
    : IQuery<IReadOnlyCollection<VaultItemDto>>;


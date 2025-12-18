using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Enums;

namespace PasswordManager.Domain.Interfaces;

/// <summary>
/// Repository interface for vault item operations.
/// Supports both local (SQLite) and remote (API) implementations.
/// </summary>
public interface IVaultRepository
{
    /// <summary>
    /// Gets a vault item by ID.
    /// </summary>
    Task<VaultItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all vault items for a user (excluding deleted items by default).
    /// </summary>
    Task<IReadOnlyList<VaultItem>> GetAllAsync(Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets vault items by type.
    /// </summary>
    Task<IReadOnlyList<VaultItem>> GetByTypeAsync(Guid userId, VaultItemType type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches vault items by name, username, or URL.
    /// </summary>
    Task<IReadOnlyList<VaultItem>> SearchAsync(Guid userId, string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets favorite vault items.
    /// </summary>
    Task<IReadOnlyList<VaultItem>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new vault item.
    /// </summary>
    Task<VaultItem> AddAsync(VaultItem item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing vault item.
    /// </summary>
    Task<VaultItem> UpdateAsync(VaultItem item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes a vault item (marks as deleted).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently deletes a vault item.
    /// </summary>
    Task PermanentlyDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets items that need to be synced to the server.
    /// </summary>
    Task<IReadOnlyList<VaultItem>> GetItemsNeedingSyncAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks an item as synced.
    /// </summary>
    Task MarkAsSyncedAsync(Guid id, CancellationToken cancellationToken = default);
}
using Microsoft.EntityFrameworkCore;
using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Enums;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Repositories;

/// <summary>
/// Local vault repository using SQLCipher-encrypted SQLite database.
/// Provides offline-first data access.
/// </summary>
public sealed class LocalVaultRepository : IVaultRepository
{
    private readonly VaultDbContext _dbContext;

    public LocalVaultRepository(VaultDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<VaultItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VaultItems
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> GetAllAsync(Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VaultItems.Where(v => v.UserId == userId);
        
        if (!includeDeleted)
            query = query.Where(v => !v.IsDeleted);

        return await query
            .OrderByDescending(v => v.IsFavorite)
            .ThenByDescending(v => v.LastModifiedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> GetByTypeAsync(Guid userId, VaultItemType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VaultItems
            .Where(v => v.UserId == userId && v.Type == type && !v.IsDeleted)
            .OrderByDescending(v => v.IsFavorite)
            .ThenByDescending(v => v.LastModifiedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> SearchAsync(Guid userId, string query, CancellationToken cancellationToken = default)
    {
        var searchTerm = query.ToLowerInvariant();
        
        return await _dbContext.VaultItems
            .Where(v => v.UserId == userId && !v.IsDeleted &&
                (EF.Functions.Like(v.Name.ToLower(), $"%{searchTerm}%") ||
                 (v.Username != null && EF.Functions.Like(v.Username.ToLower(), $"%{searchTerm}%")) ||
                 (v.Url != null && EF.Functions.Like(v.Url.ToLower(), $"%{searchTerm}%")) ||
                 (v.Notes != null && EF.Functions.Like(v.Notes.ToLower(), $"%{searchTerm}%"))))
            .OrderByDescending(v => v.IsFavorite)
            .ThenByDescending(v => v.LastModifiedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VaultItems
            .Where(v => v.UserId == userId && v.IsFavorite && !v.IsDeleted)
            .OrderByDescending(v => v.LastModifiedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<VaultItem> AddAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var newItem = item with 
        { 
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
            Version = 1
        };

        await _dbContext.VaultItems.AddAsync(newItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newItem;
    }

    public async Task<VaultItem> UpdateAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existing = await _dbContext.VaultItems
            .FirstOrDefaultAsync(v => v.Id == item.Id, cancellationToken);

        if (existing == null)
            throw new VaultItemNotFoundException(item.Id);

        var updated = item with 
        { 
            LastModifiedUtc = DateTime.UtcNow,
            Version = existing.Version + 1,
            CreatedAtUtc = existing.CreatedAtUtc // Preserve original creation time
        };

        _dbContext.VaultItems.Remove(existing);
        await _dbContext.VaultItems.AddAsync(updated, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return updated;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        
        if (item == null)
            throw new VaultItemNotFoundException(id);

        var deleted = item with 
        { 
            IsDeleted = true,
            LastModifiedUtc = DateTime.UtcNow,
            Version = item.Version + 1
        };

        _dbContext.VaultItems.Remove(item);
        await _dbContext.VaultItems.AddAsync(deleted, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PermanentlyDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.VaultItems
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (item == null)
            throw new VaultItemNotFoundException(id);

        _dbContext.VaultItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> GetItemsNeedingSyncAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VaultItems
            .Where(v => v.UserId == userId && v.NeedsSync)
            .OrderBy(v => v.LastModifiedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        
        if (item == null)
            throw new VaultItemNotFoundException(id);

        var synced = item with { NeedsSync = false };

        _dbContext.VaultItems.Remove(item);
        await _dbContext.VaultItems.AddAsync(synced, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
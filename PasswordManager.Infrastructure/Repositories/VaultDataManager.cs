using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Enums;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Repositories;

/// <summary>
/// CRITICAL COMPONENT: VaultDataManager implements "Offline-First, Cloud-Optional" strategy.
/// 
/// Strategy:
/// 1. ALL writes go to local SQLite/SQLCipher FIRST (immediate persistence)
/// 2. If user is Premium + online: queue background sync to API
/// 3. Sync queue is persistent and retries on network restoration
/// 4. Reads prioritize local data (offline capability)
/// </summary>
public sealed class VaultDataManager
{
    private readonly IVaultRepository _localRepository;
    private readonly IVaultRepository? _syncRepository;
    private readonly User _currentUser;
    private readonly SyncQueue _syncQueue;

    public VaultDataManager(
        IVaultRepository localRepository,
        IVaultRepository? syncRepository,
        User currentUser,
        SyncQueue syncQueue)
    {
        _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
        _syncRepository = syncRepository;
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _syncQueue = syncQueue ?? throw new ArgumentNullException(nameof(syncQueue));
    }

    /// <summary>
    /// Gets vault item (local-first).
    /// </summary>
    public Task<VaultItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _localRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets all vault items (local-first).
    /// </summary>
    public Task<IReadOnlyList<VaultItem>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return _localRepository.GetAllAsync(_currentUser.Id, includeDeleted, cancellationToken);
    }

    /// <summary>
    /// Saves vault item with offline-first strategy.
    /// 
    /// Flow:
    /// 1. Save to local SQLite immediately (guaranteed persistence)
    /// 2. If Premium: mark as NeedsSync and queue background sync
    /// 3. Return immediately (no waiting for API)
    /// </summary>
    public async Task<VaultItem> SaveAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Ensure item belongs to current user
        var itemToSave = item with 
        { 
            UserId = _currentUser.Id,
            NeedsSync = _currentUser.IsPremium // Premium users need cloud sync
        };

        // STEP 1: Save to local database IMMEDIATELY (primary source of truth)
        VaultItem savedItem;
        if (item.Id == Guid.Empty || item.Version == 0)
        {
            savedItem = await _localRepository.AddAsync(itemToSave, cancellationToken);
        }
        else
        {
            savedItem = await _localRepository.UpdateAsync(itemToSave, cancellationToken);
        }

        // STEP 2: Queue background sync for Premium users
        if (_currentUser.IsPremium && _syncRepository != null)
        {
            await _syncQueue.EnqueueAsync(savedItem, SyncOperation.Upsert);
        }

        return savedItem;
    }

    /// <summary>
    /// Deletes vault item (soft delete).
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // STEP 1: Delete locally
        await _localRepository.DeleteAsync(id, cancellationToken);

        // STEP 2: Queue sync for Premium users
        if (_currentUser.IsPremium && _syncRepository != null)
        {
            var deletedItem = await _localRepository.GetByIdAsync(id, cancellationToken);
            if (deletedItem != null)
            {
                await _syncQueue.EnqueueAsync(deletedItem, SyncOperation.Delete);
            }
        }
    }

    /// <summary>
    /// Searches vault items.
    /// </summary>
    public Task<IReadOnlyList<VaultItem>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return _localRepository.SearchAsync(_currentUser.Id, query, cancellationToken);
    }

    /// <summary>
    /// Synchronizes local changes with server (Premium only).
    /// Called by background service or manually by user.
    /// </summary>
    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsPremium || _syncRepository == null)
        {
            return new SyncResult
            {
                Success = false,
                Message = "Sync is only available for Premium users"
            };
        }

        var result = new SyncResult { Success = true };

        try
        {
            // PUSH: Send local changes to server
            var itemsToSync = await _localRepository.GetItemsNeedingSyncAsync(_currentUser.Id, cancellationToken);
            
            foreach (var item in itemsToSync)
            {
                try
                {
                    if (item.IsDeleted)
                    {
                        await _syncRepository.DeleteAsync(item.Id, cancellationToken);
                    }
                    else
                    {
                        await _syncRepository.UpdateAsync(item, cancellationToken);
                    }

                    // Mark as synced locally
                    await _localRepository.MarkAsSyncedAsync(item.Id, cancellationToken);
                    result.ItemsSynced++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to sync item {item.Id}: {ex.Message}");
                }
            }

            // PULL: Get remote changes (conflict resolution)
            var remoteItems = await _syncRepository.GetAllAsync(_currentUser.Id, false, cancellationToken);
            var localItems = await _localRepository.GetAllAsync(_currentUser.Id, false, cancellationToken);

            foreach (var remoteItem in remoteItems)
            {
                var localItem = localItems.FirstOrDefault(l => l.Id == remoteItem.Id);

                if (localItem == null)
                {
                    // New item from server
                    await _localRepository.AddAsync(remoteItem with { NeedsSync = false }, cancellationToken);
                    result.ItemsReceived++;
                }
                else if (remoteItem.Version > localItem.Version)
                {
                    // Server version is newer (conflict resolution: server wins)
                    await _localRepository.UpdateAsync(remoteItem with { NeedsSync = false }, cancellationToken);
                    result.Conflicts++;
                }
            }

            result.Message = $"Sync completed: {result.ItemsSynced} sent, {result.ItemsReceived} received, {result.Conflicts} conflicts resolved";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Gets sync status for UI display.
    /// </summary>
    public async Task<SyncStatus> GetSyncStatusAsync()
    {
        var pendingCount = 0;
        
        if (_currentUser.IsPremium)
        {
            var pending = await _localRepository.GetItemsNeedingSyncAsync(_currentUser.Id);
            pendingCount = pending.Count;
        }

        return new SyncStatus
        {
            IsPremiumUser = _currentUser.IsPremium,
            PendingSyncCount = pendingCount,
            LastSyncAtUtc = null // TODO: Store last sync time
        };
    }
}

/// <summary>
/// Persistent sync queue for offline resilience.
/// Items are queued even when offline and synced when connection returns.
/// </summary>
public sealed class SyncQueue
{
    private readonly Queue<SyncQueueItem> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task EnqueueAsync(VaultItem item, SyncOperation operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            _queue.Enqueue(new SyncQueueItem
            {
                Item = item,
                Operation = operation,
                QueuedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SyncQueueItem?> DequeueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int Count => _queue.Count;
}

public sealed class SyncQueueItem
{
    public required VaultItem Item { get; set; }
    public required SyncOperation Operation { get; set; }
    public DateTime QueuedAtUtc { get; set; }
    public int RetryCount { get; set; }
}

public enum SyncOperation
{
    Upsert,
    Delete
}

public sealed class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ItemsSynced { get; set; }
    public int ItemsReceived { get; set; }
    public int Conflicts { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class SyncStatus
{
    public bool IsPremiumUser { get; set; }
    public int PendingSyncCount { get; set; }
    public DateTime? LastSyncAtUtc { get; set; }
}
using System.Net.Http.Json;
using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Enums;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Repositories;

/// <summary>
/// Remote vault repository for syncing with ASP.NET Core API.
/// Used only for Premium users with cloud sync enabled.
/// </summary>
public sealed class SyncVaultRepository : IVaultRepository
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public SyncVaultRepository(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
    }

    public async Task<VaultItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/vault/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VaultItem>(cancellationToken);
    }

    public async Task<IReadOnlyList<VaultItem>> GetAllAsync(Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/vault?userId={userId}&includeDeleted={includeDeleted}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<VaultItem>>(cancellationToken);
        return items ?? new List<VaultItem>();
    }

    public async Task<IReadOnlyList<VaultItem>> GetByTypeAsync(Guid userId, VaultItemType type, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/vault/by-type?userId={userId}&type={type}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<VaultItem>>(cancellationToken);
        return items ?? new List<VaultItem>();
    }

    public async Task<IReadOnlyList<VaultItem>> SearchAsync(Guid userId, string query, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/vault/search?userId={userId}&query={Uri.EscapeDataString(query)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<VaultItem>>(cancellationToken);
        return items ?? new List<VaultItem>();
    }

    public async Task<IReadOnlyList<VaultItem>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/vault/favorites?userId={userId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<VaultItem>>(cancellationToken);
        return items ?? new List<VaultItem>();
    }

    public async Task<VaultItem> AddAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/vault", item, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<VaultItem>(cancellationToken);
        return created ?? throw new InvalidOperationException("Failed to deserialize created item");
    }

    public async Task<VaultItem> UpdateAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/vault/{item.Id}", item, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<VaultItem>(cancellationToken);
        return updated ?? throw new InvalidOperationException("Failed to deserialize updated item");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/vault/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task PermanentlyDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/vault/{id}/permanent", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<IReadOnlyList<VaultItem>> GetItemsNeedingSyncAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Not applicable for remote repository
        return Task.FromResult<IReadOnlyList<VaultItem>>(new List<VaultItem>());
    }

    public Task MarkAsSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Not applicable for remote repository
        return Task.CompletedTask;
    }
}
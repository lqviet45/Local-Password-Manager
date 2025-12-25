namespace PasswordManager.Desktop.Services;

/// <summary>
/// Interface for auto-fill operations (Desktop-only).
/// This service is NOT needed by the API backend.
/// </summary>
public interface IAutoFillService : IDisposable
{
    /// <summary>
    /// Attempts to auto-fill credentials for the current active window.
    /// </summary>
    Task<AutoFillResult> TryAutoFillAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the current browser and URL.
    /// </summary>
    Task<BrowserContext?> DetectBrowserContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds matching vault items for a given URL.
    /// </summary>
    Task<IReadOnlyList<AutoFillMatch>> FindMatchingItemsAsync(Guid userId, string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs auto-fill with a specific vault item.
    /// </summary>
    Task<bool> FillCredentialsAsync(Guid vaultItemId, CancellationToken cancellationToken = default);
}

#region DTOs

/// <summary>
/// Result of an auto-fill operation.
/// </summary>
public sealed record AutoFillResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public AutoFillErrorCode ErrorCode { get; init; }
    public BrowserContext? BrowserContext { get; init; }
    public int MatchCount { get; init; }

    public static AutoFillResult Successful(BrowserContext context, int matchCount) =>
        new()
        {
            Success = true,
            Message = "Auto-fill completed successfully",
            ErrorCode = AutoFillErrorCode.None,
            BrowserContext = context,
            MatchCount = matchCount
        };

    public static AutoFillResult Failed(AutoFillErrorCode errorCode, string message) =>
        new()
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            MatchCount = 0
        };
}

/// <summary>
/// Browser context information.
/// </summary>
public sealed record BrowserContext(
    string BrowserName,
    string Url,
    string Domain,
    string WindowTitle,
    int ProcessId);

/// <summary>
/// Matching vault item for auto-fill.
/// </summary>
public sealed record AutoFillMatch(
    Guid VaultItemId,
    string Name,
    string Username,
    string Url,
    int MatchScore);

/// <summary>
/// Auto-fill error codes.
/// </summary>
public enum AutoFillErrorCode
{
    None = 0,
    NoBrowserDetected = 1,
    UrlExtractionFailed = 2,
    NoMatchingItems = 3,
    MultipleMatches = 4,
    FillOperationFailed = 5,
    UnsupportedBrowser = 6,
    PermissionDenied = 7
}

#endregion
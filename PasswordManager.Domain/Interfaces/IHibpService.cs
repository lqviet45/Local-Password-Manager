namespace PasswordManager.Domain.Interfaces;

/// <summary>
/// Service for checking passwords against the Have I Been Pwned database.
/// Uses k-Anonymity model (only first 5 chars of SHA-1 hash are sent).
/// </summary>
public interface IHibpService
{
    /// <summary>
    /// Checks if a password has been exposed in known data breaches.
    /// Uses k-Anonymity: only sends first 5 characters of SHA-1 hash to HIBP API.
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>Breach check result</returns>
    Task<HibpCheckResult> CheckPasswordAsync(string password);
    
    /// <summary>
    /// Checks multiple passwords in a batch (more efficient).
    /// </summary>
    /// <param name="passwords">Passwords to check</param>
    /// <returns>Dictionary of password to breach result</returns>
    Task<Dictionary<string, HibpCheckResult>> CheckPasswordsBatchAsync(IEnumerable<string> passwords);
}

/// <summary>
/// Result of a HIBP breach check.
/// </summary>
public sealed record HibpCheckResult
{
    /// <summary>
    /// Indicates if password was found in breaches
    /// </summary>
    public required bool IsBreached { get; init; }
    
    /// <summary>
    /// Number of times password appeared in breaches
    /// </summary>
    public int BreachCount { get; init; }
    
    /// <summary>
    /// Timestamp of the check
    /// </summary>
    public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error message if check failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
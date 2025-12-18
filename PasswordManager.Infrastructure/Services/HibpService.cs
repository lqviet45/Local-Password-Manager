using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Services;

/// <summary>
/// Have I Been Pwned (HIBP) service using k-Anonymity model.
/// Only sends first 5 characters of SHA-1 hash to protect privacy.
/// API Documentation: https://haveibeenpwned.com/API/v3
/// </summary>
public sealed class HibpService : IHibpService
{
    private readonly HttpClient _httpClient;
    private const string HibpApiBaseUrl = "https://api.pwnedpasswords.com/range/";
    private const int HashPrefixLength = 5;

    public HibpService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(HibpApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PasswordManager-HIBP-Client");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<HibpCheckResult> CheckPasswordAsync(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        try
        {
            var sha1Hash = ComputeSha1Hash(password);
            var hashPrefix = sha1Hash[..HashPrefixLength];
            var hashSuffix = sha1Hash[HashPrefixLength..];

            // Send only first 5 characters to HIBP (k-Anonymity)
            var response = await _httpClient.GetAsync(hashPrefix);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var breachCount = ParseBreachCount(responseBody, hashSuffix);

            return new HibpCheckResult
            {
                IsBreached = breachCount > 0,
                BreachCount = breachCount,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            return new HibpCheckResult
            {
                IsBreached = false,
                BreachCount = 0,
                ErrorMessage = $"Failed to check HIBP: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new HibpCheckResult
            {
                IsBreached = false,
                BreachCount = 0,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<Dictionary<string, HibpCheckResult>> CheckPasswordsBatchAsync(IEnumerable<string> passwords)
    {
        ArgumentNullException.ThrowIfNull(passwords);

        var results = new Dictionary<string, HibpCheckResult>();
        
        // Group passwords by hash prefix to minimize API calls
        var passwordsByPrefix = passwords
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .GroupBy(p => ComputeSha1Hash(p)[..HashPrefixLength])
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (prefix, passwordList) in passwordsByPrefix)
        {
            try
            {
                var response = await _httpClient.GetAsync(prefix);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                foreach (var password in passwordList)
                {
                    var sha1Hash = ComputeSha1Hash(password);
                    var hashSuffix = sha1Hash[HashPrefixLength..];
                    var breachCount = ParseBreachCount(responseBody, hashSuffix);

                    results[password] = new HibpCheckResult
                    {
                        IsBreached = breachCount > 0,
                        BreachCount = breachCount,
                        CheckedAtUtc = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                // Add error result for all passwords in this batch
                foreach (var password in passwordList)
                {
                    results[password] = new HibpCheckResult
                    {
                        IsBreached = false,
                        BreachCount = 0,
                        ErrorMessage = $"Failed to check: {ex.Message}"
                    };
                }
            }

            // Rate limiting: avoid overwhelming the API
            await Task.Delay(100);
        }

        return results;
    }

    /// <summary>
    /// Computes SHA-1 hash of password (HIBP requirement).
    /// Note: SHA-1 is only used for HIBP compatibility, not for password storage.
    /// </summary>
    private static string ComputeSha1Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Parses HIBP API response to find breach count for a specific hash suffix.
    /// Response format: "HASH_SUFFIX:COUNT\r\n"
    /// </summary>
    private static int ParseBreachCount(string responseBody, string hashSuffix)
    {
        var lines = responseBody.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2 && 
                string.Equals(parts[0], hashSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(parts[1], out var count) ? count : 0;
            }
        }

        return 0; // Not found in breaches
    }
}
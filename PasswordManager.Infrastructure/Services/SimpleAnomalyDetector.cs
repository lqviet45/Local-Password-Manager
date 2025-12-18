using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Services;

/// <summary>
/// Simple rule-based anomaly detector (C# implementation).
/// FUTURE: Replace with Python AI microservice via gRPC or RabbitMQ.
/// 
/// Architecture notes for future AI integration:
/// 1. Keep this interface (IAnomalyDetector) unchanged
/// 2. Create AnomalyDetectorGrpcClient : IAnomalyDetector
/// 3. Swap implementation in DI container
/// 4. Python service will use ML models (isolation forest, LSTM, etc.)
/// </summary>
public sealed class SimpleAnomalyDetector : IAnomalyDetector
{
    private readonly IVaultRepository _vaultRepository;
    
    // Simple thresholds for rule-based detection
    private const int MaxFailedLoginAttempts = 5;
    private const int SuspiciousLoginTimeWindowMinutes = 15;
    private const int UnusualActivityThreshold = 10;

    public SimpleAnomalyDetector(IVaultRepository vaultRepository)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
    }

    public async Task<AnomalyDetectionResult> AnalyzeUserBehaviorAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var anomalies = new List<string>();
        var recommendations = new List<string>();
        var riskScore = 0;

        try
        {
            // Get user's vault items
            var items = await _vaultRepository.GetAllAsync(userId, includeDeleted: false, cancellationToken);
            
            // Rule 1: Check for too many items created in short time
            var recentItems = items.Where(i => 
                i.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-SuspiciousLoginTimeWindowMinutes))
                .ToList();
            
            if (recentItems.Count() > UnusualActivityThreshold)
            {
                anomalies.Add($"Unusual number of vault items created recently ({recentItems.Count()})");
                riskScore += 30;
                recommendations.Add("Review recently added items for unauthorized changes");
            }

            // Rule 2: Check for mass deletion
            var recentlyDeleted = items.Where(i => 
                i.IsDeleted && i.LastModifiedUtc > DateTime.UtcNow.AddMinutes(-SuspiciousLoginTimeWindowMinutes));
            
            if (recentlyDeleted.Count() > 5)
            {
                anomalies.Add($"Multiple items deleted recently ({recentlyDeleted.Count()})");
                riskScore += 40;
                recommendations.Add("Verify if deletions were intentional");
            }

            // Rule 3: Check for items needing sync (could indicate sync issues)
            var needingSync = items.Where(i => i.NeedsSync);
            if (needingSync.Count() > items.Count / 2)
            {
                anomalies.Add("Large number of items pending synchronization");
                riskScore += 20;
                recommendations.Add("Check your internet connection and sync status");
            }

            // TODO: Add more sophisticated rules:
            // - Geo-location anomalies (requires IP tracking)
            // - Time-based access patterns
            // - Device fingerprinting changes
            // - Password reuse detection

            return new AnomalyDetectionResult
            {
                RiskScore = Math.Min(riskScore, 100),
                RequiresAction = riskScore >= 70,
                Anomalies = anomalies,
                Recommendations = recommendations,
                AnalyzedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new AnomalyDetectionResult
            {
                RiskScore = 0,
                RequiresAction = false,
                Anomalies = new List<string> { $"Analysis failed: {ex.Message}" },
                Recommendations = new List<string> { "Unable to perform anomaly detection" }
            };
        }
    }

    public Task<bool> IsSuspiciousLoginAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Simple implementation: check IP format validity
        // FUTURE AI: Analyze historical login patterns, geo-location, device fingerprinting
        
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Task.FromResult(true);

        // Basic validation only
        var isSuspicious = false;
        
        // Check for localhost/private IPs (might be suspicious in production)
        if (ipAddress.StartsWith("127.") || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."))
            isSuspicious = false; // Allow for development
        
        return Task.FromResult(isSuspicious);
    }

    public Task<bool> DetectCredentialStuffingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: always return false
        // FUTURE AI: Analyze login attempt patterns, rate, and distribution
        // - High frequency of failed logins
        // - Logins from multiple IPs in short time
        // - Pattern matching known bot behavior
        
        return Task.FromResult(false);
    }
}

/* 
 * FUTURE PYTHON AI INTEGRATION GUIDE:
 * 
 * 1. Create Python gRPC Service:
 *    - Define .proto file with IAnomalyDetector methods
 *    - Implement ML models (Isolation Forest for outliers, LSTM for sequences)
 *    - Train models on: login times, IP patterns, vault activity
 * 
 * 2. Create C# gRPC Client:
 *    public class AnomalyDetectorGrpcClient : IAnomalyDetector
 *    {
 *        private readonly AnomalyDetectorService.AnomalyDetectorServiceClient _client;
 *        
 *        public async Task<AnomalyDetectionResult> AnalyzeUserBehaviorAsync(...)
 *        {
 *            var request = new AnalyzeUserBehaviorRequest { UserId = userId.ToString() };
 *            var response = await _client.AnalyzeUserBehaviorAsync(request);
 *            return MapFromGrpcResponse(response);
 *        }
 *    }
 * 
 * 3. Swap in DI Container (program.cs or Startup.cs):
 *    services.AddSingleton<IAnomalyDetector, AnomalyDetectorGrpcClient>();
 * 
 * 4. Alternative: RabbitMQ Message-Based:
 *    - Publish analysis requests to queue
 *    - Python worker consumes messages
 *    - Results published back to response queue
 */
namespace PasswordManager.Domain.Interfaces;

/// <summary>
/// Interface for AI-powered anomaly detection.
/// Current implementation: Simple rule-based logic (C#)
/// Future: Python microservice via gRPC/RabbitMQ
/// </summary>
public interface IAnomalyDetector
{
    /// <summary>
    /// Analyzes user behavior for anomalies.
    /// </summary>
    /// <param name="userId">User to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly detection result</returns>
    Task<AnomalyDetectionResult> AnalyzeUserBehaviorAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a login attempt is suspicious.
    /// </summary>
    /// <param name="userId">User attempting login</param>
    /// <param name="ipAddress">IP address of the attempt</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if login is suspicious</returns>
    Task<bool> IsSuspiciousLoginAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detects patterns indicating credential stuffing attacks.
    /// </summary>
    /// <param name="userId">User to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credential stuffing detected</returns>
    Task<bool> DetectCredentialStuffingAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of anomaly detection analysis.
/// </summary>
public sealed record AnomalyDetectionResult
{
    /// <summary>
    /// Overall risk score (0-100, higher = more suspicious)
    /// </summary>
    public required int RiskScore { get; init; }
    
    /// <summary>
    /// Indicates if immediate action is recommended
    /// </summary>
    public bool RequiresAction { get; init; }
    
    /// <summary>
    /// List of detected anomalies
    /// </summary>
    public List<string> Anomalies { get; init; } = new();
    
    /// <summary>
    /// Recommended actions for user
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAtUtc { get; init; } = DateTime.UtcNow;
}
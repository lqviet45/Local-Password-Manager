using System.Text.RegularExpressions;
using PasswordManager.Domain.Enums;
using PasswordManager.Domain.Interfaces;

namespace PasswordManager.Infrastructure.Services;

/// <summary>
/// Evaluates password strength using entropy calculation and pattern analysis.
/// Based on NIST SP 800-63B guidelines and zxcvbn principles.
/// </summary>
public sealed partial class PasswordStrengthService : IPasswordStrengthService
{
    // Common weak passwords and patterns
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "12345678", "qwerty", "abc123", "monkey", "letmein",
        "trustno1", "dragon", "baseball", "iloveyou", "master", "sunshine", "ashley",
        "bailey", "passw0rd", "shadow", "123123", "654321", "superman", "qazwsx"
    };

    [GeneratedRegex(@"(.)\1{2,}")]
    private static partial Regex RepeatingCharsRegex();
    
    [GeneratedRegex(@"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz|012|123|234|345|456|567|678|789)", RegexOptions.IgnoreCase)]
    private static partial Regex SequentialCharsRegex();

    public double CalculateEntropy(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        var poolSize = GetCharacterPoolSize(password);
        var entropy = password.Length * Math.Log2(poolSize);
        
        // Apply penalties for patterns
        entropy = ApplyPatternPenalties(password, entropy);
        
        return Math.Max(0, entropy);
    }

    public int CalculateStrengthScore(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        var entropy = CalculateEntropy(password);
        var score = (int)(entropy * 0.8); // Base score from entropy
        
        // Bonus points for good practices
        if (password.Length >= 16) score += 10;
        if (password.Length >= 20) score += 5;
        if (HasMixedCase(password)) score += 5;
        if (HasDigits(password)) score += 5;
        if (HasSpecialChars(password)) score += 5;
        
        // Penalties
        if (CommonPasswords.Contains(password)) score = Math.Min(score, 20);
        if (RepeatingCharsRegex().IsMatch(password)) score -= 10;
        if (SequentialCharsRegex().IsMatch(password)) score -= 10;
        if (password.Length < 8) score -= 20;
        
        return Math.Clamp(score, 0, 100);
    }

    public StrengthLevel EvaluateStrength(string password)
    {
        var entropy = CalculateEntropy(password);
        
        return entropy switch
        {
            < 40 => StrengthLevel.VeryWeak,
            < 60 => StrengthLevel.Weak,
            < 80 => StrengthLevel.Fair,
            < 100 => StrengthLevel.Strong,
            _ => StrengthLevel.VeryStrong
        };
    }

    public PasswordStrengthAnalysis AnalyzePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordStrengthAnalysis
            {
                Entropy = 0,
                Score = 0,
                Level = StrengthLevel.VeryWeak,
                Length = 0,
                Suggestions = new List<string> { "Password cannot be empty" }
            };
        }

        var entropy = CalculateEntropy(password);
        var score = CalculateStrengthScore(password);
        var level = EvaluateStrength(password);
        
        var analysis = new PasswordStrengthAnalysis
        {
            Entropy = entropy,
            Score = score,
            Level = level,
            Length = password.Length,
            HasUppercase = password.Any(char.IsUpper),
            HasLowercase = password.Any(char.IsLower),
            HasDigits = password.Any(char.IsDigit),
            HasSpecialChars = password.Any(c => !char.IsLetterOrDigit(c)),
            HasRepeatingChars = RepeatingCharsRegex().IsMatch(password),
            HasSequentialChars = SequentialCharsRegex().IsMatch(password)
        };

        // Generate suggestions
        var suggestions = new List<string>();
        
        if (password.Length < 12)
            suggestions.Add("Use at least 12 characters (16+ recommended)");
        
        if (!analysis.HasUppercase)
            suggestions.Add("Add uppercase letters");
        
        if (!analysis.HasLowercase)
            suggestions.Add("Add lowercase letters");
        
        if (!analysis.HasDigits)
            suggestions.Add("Add numbers");
        
        if (!analysis.HasSpecialChars)
            suggestions.Add("Add special characters (!@#$%^&*)");
        
        if (analysis.HasRepeatingChars)
            suggestions.Add("Avoid repeating characters (e.g., 'aaa', '111')");
        
        if (analysis.HasSequentialChars)
            suggestions.Add("Avoid sequential characters (e.g., 'abc', '123')");
        
        if (CommonPasswords.Contains(password))
            suggestions.Add("This is a commonly used password. Choose something unique.");
        
        if (level >= StrengthLevel.Strong && suggestions.Count == 0)
            suggestions.Add("Excellent password! Consider using a passphrase for even better security.");

        analysis = analysis with { Suggestions = suggestions };
        
        return analysis;
    }

    private static int GetCharacterPoolSize(string password)
    {
        var poolSize = 0;
        
        if (password.Any(char.IsLower)) poolSize += 26;
        if (password.Any(char.IsUpper)) poolSize += 26;
        if (password.Any(char.IsDigit)) poolSize += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) poolSize += 32; // Common special chars
        
        return Math.Max(poolSize, 1);
    }

    private static double ApplyPatternPenalties(string password, double entropy)
    {
        // Penalty for repeating characters
        if (RepeatingCharsRegex().IsMatch(password))
            entropy *= 0.7;
        
        // Penalty for sequential characters
        if (SequentialCharsRegex().IsMatch(password))
            entropy *= 0.8;
        
        // Penalty for common passwords
        if (CommonPasswords.Contains(password))
            entropy = Math.Min(entropy, 30);
        
        // Penalty for keyboard patterns (simplified)
        if (password.Contains("qwerty", StringComparison.OrdinalIgnoreCase) ||
            password.Contains("asdf", StringComparison.OrdinalIgnoreCase))
            entropy *= 0.6;
        
        return entropy;
    }

    private static bool HasMixedCase(string password) =>
        password.Any(char.IsUpper) && password.Any(char.IsLower);

    private static bool HasDigits(string password) =>
        password.Any(char.IsDigit);

    private static bool HasSpecialChars(string password) =>
        password.Any(c => !char.IsLetterOrDigit(c));
}
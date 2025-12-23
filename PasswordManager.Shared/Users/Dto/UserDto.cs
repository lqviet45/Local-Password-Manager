namespace PasswordManager.Shared.Users.Dto;

/// <summary>
/// User data contract exposed to UI/Application layers.
/// </summary>
public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public bool IsPremium { get; init; }
    public bool EmailVerified { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginUtc { get; init; }
}


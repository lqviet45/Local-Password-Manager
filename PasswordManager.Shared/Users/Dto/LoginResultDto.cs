namespace PasswordManager.Shared.Users.Dto;

/// <summary>
/// Authentication result returned by login/registration flows.
/// </summary>
public sealed record LoginResultDto
{
    public required UserDto User { get; init; }
    public required byte[] Salt { get; init; }
    public required string EncryptedMasterKey { get; init; }
}


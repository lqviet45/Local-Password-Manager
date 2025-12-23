using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Users.Dto;

namespace PasswordManager.Shared.Users.Commands.Register;

/// <summary>
/// Register a new local user with master password.
/// </summary>
public sealed record RegisterUserCommand(string Email, string MasterPassword) : ICommand<LoginResultDto>;


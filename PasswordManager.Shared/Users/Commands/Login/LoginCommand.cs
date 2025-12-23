using PasswordManager.Shared.Core.Message;
using PasswordManager.Shared.Users.Dto;

namespace PasswordManager.Shared.Users.Commands.Login;

/// <summary>
/// Authenticate a user with email and master password.
/// </summary>
public sealed record LoginCommand(string Email, string MasterPassword) : ICommand<LoginResultDto>;


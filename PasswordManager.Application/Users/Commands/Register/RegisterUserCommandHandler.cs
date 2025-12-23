using Microsoft.Extensions.Logging;
using PasswordManager.Application.Common.Mapping;
using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Users.Commands.Register;
using PasswordManager.Shared.Users.Dto;

namespace PasswordManager.Application.Users.Commands.Register;

public sealed class RegisterUserCommandHandler : PasswordManager.Shared.Core.Message.ICommandHandler<RegisterUserCommand, LoginResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IPasswordStrengthService _passwordStrengthService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        ICryptoProvider cryptoProvider,
        IPasswordStrengthService passwordStrengthService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _passwordStrengthService = passwordStrengthService ??
                                   throw new ArgumentNullException(nameof(passwordStrengthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<LoginResultDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Result<LoginResultDto>.Failure("Email is already registered");
        }

        var strength = _passwordStrengthService.EvaluateStrength(request.MasterPassword);
        if (strength < Domain.Enums.StrengthLevel.Fair)
        {
            return Result<LoginResultDto>.Failure(
                "Master password is too weak. Please choose a stronger password.");
        }

        var passwordHash = await _cryptoProvider.HashPasswordAsync(request.MasterPassword);
        var masterKey = _cryptoProvider.GenerateRandomKey(32);
        var (encryptionKey, salt) = await _cryptoProvider.DeriveKeyAsync(request.MasterPassword);
        var encryptedMasterKey = await _cryptoProvider.EncryptAsync(Convert.ToBase64String(masterKey), encryptionKey);

        var user = new User
        {
            Email = email,
            MasterPasswordHash = passwordHash,
            Salt = salt,
            EncryptedMasterKey = encryptedMasterKey.ToCombinedString(),
            IsPremium = false,
            EmailVerified = false,
            TwoFactorEnabled = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        user = await _userRepository.AddAsync(user, cancellationToken);

        var dto = new LoginResultDto
        {
            User = user.ToDto(),
            Salt = user.Salt,
            EncryptedMasterKey = user.EncryptedMasterKey
        };

        _logger.LogInformation("User registered: {Email}", email);
        return Result<LoginResultDto>.Success(dto);
    }
}


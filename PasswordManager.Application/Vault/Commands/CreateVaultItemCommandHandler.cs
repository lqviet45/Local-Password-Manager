using Microsoft.Extensions.Logging;
using PasswordManager.Application.Common.Mapping;
using PasswordManager.Domain.Entities;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Commands;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Application.Vault.Commands;

public sealed class CreateVaultItemCommandHandler : PasswordManager.Shared.Core.Message.ICommandHandler<CreateVaultItemCommand, VaultItemDto>
{
    private readonly IVaultRepository _vaultRepository;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly ILogger<CreateVaultItemCommandHandler> _logger;

    public CreateVaultItemCommandHandler(
        IVaultRepository vaultRepository,
        ICryptoProvider cryptoProvider,
        ILogger<CreateVaultItemCommandHandler> logger)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<VaultItemDto>> Handle(CreateVaultItemCommand request, CancellationToken cancellationToken)
    {
        var encrypted = await _cryptoProvider.EncryptAsync(request.Item.Password, request.EncryptionKey);
        var encryptedString = encrypted.ToCombinedString();
        var dataHash = _cryptoProvider.ComputeHash(encryptedString);

        var entity = new VaultItem
        {
            UserId = request.UserId,
            Type = request.Item.Type,
            Name = request.Item.Name.Trim(),
            Username = string.IsNullOrWhiteSpace(request.Item.Username) ? null : request.Item.Username.Trim(),
            EncryptedData = encryptedString,
            Url = string.IsNullOrWhiteSpace(request.Item.Url) ? null : request.Item.Url.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Item.Notes) ? null : request.Item.Notes.Trim(),
            Tags = string.IsNullOrWhiteSpace(request.Item.Tags) ? null : request.Item.Tags.Trim(),
            IsFavorite = request.Item.IsFavorite,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
            DataHash = dataHash
        };

        var saved = await _vaultRepository.AddAsync(entity, cancellationToken);
        _logger.LogInformation("Vault item created: {ItemId}", saved.Id);

        return Result<VaultItemDto>.Success(saved.ToDto());
    }
}


using Microsoft.Extensions.Logging;
using PasswordManager.Application.Common.Mapping;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Commands;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Application.Vault.Commands;

public sealed class UpdateVaultItemCommandHandler : PasswordManager.Shared.Core.Message.ICommandHandler<UpdateVaultItemCommand, VaultItemDto>
{
    private readonly IVaultRepository _vaultRepository;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly ILogger<UpdateVaultItemCommandHandler> _logger;

    public UpdateVaultItemCommandHandler(
        IVaultRepository vaultRepository,
        ICryptoProvider cryptoProvider,
        ILogger<UpdateVaultItemCommandHandler> logger)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<VaultItemDto>> Handle(UpdateVaultItemCommand request, CancellationToken cancellationToken)
    {
        var existing = await _vaultRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (existing == null || existing.UserId != request.UserId)
        {
            throw new VaultItemNotFoundException(request.ItemId);
        }

        var encrypted = await _cryptoProvider.EncryptAsync(request.Item.Password, request.EncryptionKey);
        var encryptedString = encrypted.ToCombinedString();
        var dataHash = _cryptoProvider.ComputeHash(encryptedString);

        var updated = existing with
        {
            Type = request.Item.Type,
            Name = request.Item.Name.Trim(),
            Username = string.IsNullOrWhiteSpace(request.Item.Username) ? null : request.Item.Username.Trim(),
            EncryptedData = encryptedString,
            Url = string.IsNullOrWhiteSpace(request.Item.Url) ? null : request.Item.Url.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Item.Notes) ? null : request.Item.Notes.Trim(),
            Tags = string.IsNullOrWhiteSpace(request.Item.Tags) ? null : request.Item.Tags.Trim(),
            IsFavorite = request.Item.IsFavorite,
            DataHash = dataHash
        };

        updated = await _vaultRepository.UpdateAsync(updated, cancellationToken);
        _logger.LogInformation("Vault item updated: {ItemId}", updated.Id);

        return Result<VaultItemDto>.Success(updated.ToDto());
    }
}


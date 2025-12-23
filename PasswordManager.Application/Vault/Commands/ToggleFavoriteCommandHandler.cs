using Microsoft.Extensions.Logging;
using PasswordManager.Application.Common.Mapping;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Commands;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Application.Vault.Commands;

public sealed class ToggleFavoriteCommandHandler : PasswordManager.Shared.Core.Message.ICommandHandler<ToggleFavoriteCommand, VaultItemDto>
{
    private readonly IVaultRepository _vaultRepository;
    private readonly ILogger<ToggleFavoriteCommandHandler> _logger;

    public ToggleFavoriteCommandHandler(
        IVaultRepository vaultRepository,
        ILogger<ToggleFavoriteCommandHandler> logger)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<VaultItemDto>> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        var existing = await _vaultRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (existing == null)
        {
            throw new VaultItemNotFoundException(request.ItemId);
        }

        var updated = existing with
        {
            IsFavorite = !existing.IsFavorite
        };

        updated = await _vaultRepository.UpdateAsync(updated, cancellationToken);
        _logger.LogInformation("Toggled favorite for item {ItemId} to {Value}", updated.Id, updated.IsFavorite);

        return Result<VaultItemDto>.Success(updated.ToDto());
    }
}


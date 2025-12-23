using Microsoft.Extensions.Logging;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Commands;

namespace PasswordManager.Application.Vault.Commands;

public sealed class DeleteVaultItemCommandHandler : PasswordManager.Shared.Core.Message.ICommandHandler<DeleteVaultItemCommand>
{
    private readonly Domain.Interfaces.IVaultRepository _vaultRepository;
    private readonly ILogger<DeleteVaultItemCommandHandler> _logger;

    public DeleteVaultItemCommandHandler(
        Domain.Interfaces.IVaultRepository vaultRepository,
        ILogger<DeleteVaultItemCommandHandler> logger)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(DeleteVaultItemCommand request, CancellationToken cancellationToken)
    {
        await _vaultRepository.DeleteAsync(request.ItemId, cancellationToken);
        _logger.LogInformation("Vault item deleted: {ItemId}", request.ItemId);
        return Result.Success();
    }
}


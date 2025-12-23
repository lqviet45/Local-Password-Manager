using Microsoft.Extensions.Logging;
using PasswordManager.Application.Common.Mapping;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Dto;
using PasswordManager.Shared.Vault.Queries;

namespace PasswordManager.Application.Vault.Queries;

public sealed class GetVaultItemsQueryHandler : PasswordManager.Shared.Core.Message.IQueryHandler<GetVaultItemsQuery, IReadOnlyCollection<VaultItemDto>>
{
    private readonly IVaultRepository _vaultRepository;
    private readonly ILogger<GetVaultItemsQueryHandler> _logger;

    public GetVaultItemsQueryHandler(IVaultRepository vaultRepository, ILogger<GetVaultItemsQueryHandler> logger)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyCollection<VaultItemDto>>> Handle(GetVaultItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _vaultRepository.GetAllAsync(request.UserId, request.IncludeDeleted, cancellationToken);
        var dtos = items.Select(i => i.ToDto()).ToList();

        _logger.LogInformation("Loaded {Count} vault items for user {UserId}", dtos.Count, request.UserId);

        return Result<IReadOnlyCollection<VaultItemDto>>.Success(dtos);
    }
}


using PasswordManager.Domain.Entities;
using PasswordManager.Shared.Users.Dto;

namespace PasswordManager.Application.Common.Mapping;

internal static class UserMapping
{
    public static UserDto ToDto(this User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            IsPremium = user.IsPremium,
            EmailVerified = user.EmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginUtc = user.LastLoginUtc
        };
}


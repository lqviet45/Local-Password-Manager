using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Infrastructure.Cryptography;
using PasswordManager.Infrastructure.Repositories;
using PasswordManager.Infrastructure.Services;

namespace PasswordManager.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services for WPF Desktop Client.
    /// </summary>
    public static IServiceCollection AddInfrastructureForDesktop(
        this IServiceCollection services,
        string sqlCipherPassword)
    {
        // Cryptography
        services.AddSingleton<ICryptoProvider, CryptoProvider>();
        
        // Password Services
        services.AddSingleton<IPasswordStrengthService, PasswordStrengthService>();
        services.AddHttpClient<IHibpService, HibpService>();
        
        // Database (SQLCipher)
        services.AddDbContext<VaultDbContext>(options =>
        {
            VaultDbContext.ConfigureSqlCipher(options, sqlCipherPassword);
        });
        
        // Repositories
        services.AddScoped<IVaultRepository, LocalVaultRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Sync Infrastructure
        services.AddSingleton<SyncQueue>();
        
        // Anomaly Detection (Simple C# version)
        services.AddScoped<IAnomalyDetector, SimpleAnomalyDetector>();

        return services;
    }

    /// <summary>
    /// Registers sync repository for Premium users.
    /// </summary>
    public static IServiceCollection AddSyncRepositoryForPremium(
        this IServiceCollection services,
        string apiBaseUrl)
    {
        services.AddHttpClient<IVaultRepository, SyncVaultRepository>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Registers all Infrastructure services for ASP.NET Core API.
    /// </summary>
    public static IServiceCollection AddInfrastructureForApi(
        this IServiceCollection services,
        string connectionString)
    {
        // Cryptography
        services.AddSingleton<ICryptoProvider, CryptoProvider>();
        
        // Password Services
        services.AddSingleton<IPasswordStrengthService, PasswordStrengthService>();
        services.AddHttpClient<IHibpService, HibpService>();
        
        // Database (PostgreSQL)
        services.AddDbContext<VaultDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        // Repositories
        services.AddScoped<IVaultRepository, LocalVaultRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Anomaly Detection
        services.AddScoped<IAnomalyDetector, SimpleAnomalyDetector>();

        return services;
    }

    /// <summary>
    /// Creates and migrates the database.
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VaultDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
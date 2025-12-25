using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Desktop.Services;
using PasswordManager.Desktop.Services.Impl;

namespace PasswordManager.Desktop.Extensions;

/// <summary>
/// Extension methods for registering desktop-specific services.
/// Follows Dependency Injection and Interface Segregation principles.
/// </summary>
public static class DesktopServiceExtensions
{
    /// <summary>
    /// Registers all desktop UI services following SOLID principles.
    /// All services are registered as Singleton for performance and state management.
    /// </summary>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        // System Tray Service (D: Dependency Inversion - depends on interface)
        services.AddSingleton<ISystemTrayService, SystemTrayService>();

        // Global Hotkey Service (D: Dependency Inversion)
        services.AddSingleton<IGlobalHotKeyService, GlobalHotKeyService>();

        // Secure Clipboard Service (D: Dependency Inversion)
        // BACKWARD COMPATIBLE with existing IClipboardService
        services.AddSingleton<IClipboardService, ClipboardService>();

        // TODO: Uncomment when implementations are complete
        // services.AddSingleton<IAutoFillService, AutoFillService>();
        // services.AddSingleton<IBrowserExtensionCommunicator, BrowserExtensionCommunicator>();

        return services;
    }
}
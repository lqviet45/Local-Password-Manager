using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.Services;
using PasswordManager.Desktop.Services.Impl;
using PasswordManager.Desktop.ViewModels;
using PasswordManager.Desktop.Views;
using InfrastructureDI = PasswordManager.Infrastructure.DependencyInjection;
using ApplicationDI = PasswordManager.Application.DependencyInjection;
using Serilog;

namespace PasswordManager.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// Configures Dependency Injection and application lifetime.
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IHost _host;
    
    // Desktop-specific services
    private ISystemTrayService? _systemTrayService;
    private IGlobalHotKeyService? _hotKeyService;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .UseSerilog((context, loggerConfig) =>
            {
                loggerConfig
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: "logs/app.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Register Configuration
        services.AddSingleton(configuration);

        // Application + Infrastructure
        ApplicationDI.AddApplication(services);
        InfrastructureDI.AddInfrastructureForDesktop(services, "temporary_password_will_be_replaced");

        // Application Services
        services.AddSingleton<IMasterPasswordService, MasterPasswordService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IDialogService, DialogService>();
        
        // Desktop-specific services (System Tray, Hotkeys, Clipboard)
        services.AddSingleton<ISystemTrayService, SystemTrayService>();
        services.AddSingleton<IGlobalHotKeyService, GlobalHotKeyService>();
        services.AddSingleton<IClipboardService, ClipboardService>(); // Enhanced clipboard with auto-clear
        
        services.AddSingleton<IWindowFactory, WindowFactory>();

        // ViewModels - Transient (new instance each time)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AddEditItemViewModel>();
        services.AddSingleton<VaultViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views - Transient
        services.AddTransient<LoginWindow>();
        
        // Add Logging
        services.AddLogging();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        // Get logger
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("=== APPLICATION STARTUP ===");

        // Initialize database
        var serviceProvider = _host.Services;
        try
        {
            await InfrastructureDI.InitializeDatabaseAsync(serviceProvider);
            logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            MessageBox.Show($"Failed to initialize database: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Initialize desktop-specific services
        try
        {
            _systemTrayService = serviceProvider.GetRequiredService<ISystemTrayService>();
            _hotKeyService = serviceProvider.GetRequiredService<IGlobalHotKeyService>();
            logger.LogInformation("Desktop services initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize desktop services (non-critical)");
            // Continue without desktop features
        }

        // Show login window
        try
        {
            var windowFactory = serviceProvider.GetRequiredService<IWindowFactory>();
            var loginWindow = windowFactory.CreateLoginWindow();
            loginWindow.Show();
            logger.LogInformation("Login window displayed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show login window");
            MessageBox.Show($"Failed to show login window: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("=== APPLICATION SHUTDOWN ===");

        // Dispose desktop services
        try
        {
            _systemTrayService?.Dispose();
            _hotKeyService?.Dispose();
            logger.LogInformation("Desktop services disposed successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing desktop services");
        }

        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Initializes system tray and hotkeys for the main window.
    /// Call this from MainWindow.OnLoaded event.
    /// </summary>
    public void InitializeDesktopFeatures(Window mainWindow)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();

        if (_systemTrayService == null || _hotKeyService == null)
        {
            logger.LogWarning("Cannot initialize desktop features: Services not available");
            return;
        }

        try
        {
            // Initialize hotkey service with main window handle
            var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
            _hotKeyService.Initialize(helper.Handle);

            // Register default hotkeys
            // Ctrl+Shift+L - Show/Hide Password Manager
            _hotKeyService.RegisterHotKey(
                "toggle-window",
                HotKeyModifiers.Control | HotKeyModifiers.Shift,
                0x4C, // VK_L
                () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (mainWindow.IsVisible && mainWindow.WindowState != WindowState.Minimized)
                        {
                            _systemTrayService.MinimizeToTray();
                        }
                        else
                        {
                            _systemTrayService.RestoreFromTray();
                        }
                    });
                },
                "Toggle Password Manager Window (Ctrl+Shift+L)"
            );

            // Ctrl+Shift+C - Copy password (will be implemented in VaultViewModel)
            _hotKeyService.RegisterHotKey(
                "copy-password",
                HotKeyModifiers.Control | HotKeyModifiers.Shift,
                0x43, // VK_C
                () =>
                {
                    logger.LogDebug("Copy password hotkey triggered");
                    // TODO: Implement copy selected password from vault
                },
                "Copy Selected Password (Ctrl+Shift+C)"
            );

            logger.LogInformation("Desktop features (System Tray + Hotkeys) initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize desktop features");
        }
    }

    public static IServiceProvider ServiceProvider => ((App)Current)._host.Services;

    /// <summary>
    /// Gets the system tray service instance.
    /// </summary>
    public ISystemTrayService? SystemTray => _systemTrayService;

    /// <summary>
    /// Gets the hotkey service instance.
    /// </summary>
    public IGlobalHotKeyService? HotKeys => _hotKeyService;
}
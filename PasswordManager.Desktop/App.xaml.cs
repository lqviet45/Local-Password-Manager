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
        services.AddSingleton<IClipboardService, ClipboardService>();
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
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }

    public static IServiceProvider ServiceProvider => ((App)Current)._host.Services;
}
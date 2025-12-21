using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.Views;

namespace PasswordManager.Desktop.Services.Impl;

public sealed class WindowFactory : IWindowFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WindowFactory> _logger;

    public WindowFactory(IServiceProvider serviceProvider, ILogger<WindowFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LoginWindow CreateLoginWindow()
    {
        _logger.LogInformation("Creating LoginWindow...");
        try
        {
            var window = _serviceProvider.GetRequiredService<LoginWindow>();
            _logger.LogInformation("✓ LoginWindow created successfully");
            return window;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create LoginWindow");
            throw;
        }
    }

    public MainWindow CreateMainWindow()
    {
        _logger.LogInformation("Creating MainWindow...");
        try
        {
            // Try to get MainWindow from DI
            var window = _serviceProvider.GetService<MainWindow>();
            
            if (window == null)
            {
                _logger.LogWarning("MainWindow not found in DI, creating manually...");
                window = new MainWindow();
            }
            else
            {
                _logger.LogInformation("✓ MainWindow resolved from DI");
            }
            
            _logger.LogInformation("MainWindow.DataContext: {Type}", 
                window.DataContext?.GetType().Name ?? "NULL");
            
            return window;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MainWindow, falling back to manual creation");
            return new MainWindow();
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.ViewModels;
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
        _logger.LogInformation("Creating MainWindow via Factory...");
        try
        {
            // Get MainViewModel from DI (this is already fully constructed)
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            _logger.LogInformation("✓ MainViewModel resolved: {Type}", mainViewModel.GetType().Name);
            _logger.LogInformation("  - CurrentViewModel: {Type}", 
                mainViewModel.CurrentViewModel?.GetType().Name ?? "NULL");
            
            // Get Logger for MainWindow
            var logger = _serviceProvider.GetRequiredService<ILogger<MainWindow>>();
            
            // Create MainWindow manually with ViewModel
            var window = new MainWindow(mainViewModel, logger);
            
            _logger.LogInformation("✓ MainWindow created successfully");
            _logger.LogInformation("  - DataContext: {Type}", 
                window.DataContext?.GetType().Name ?? "NULL");
            
            return window;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MainWindow");
            throw;
        }
    }
}
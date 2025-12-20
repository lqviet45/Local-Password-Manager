using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Desktop.Views;

namespace PasswordManager.Desktop.Services.Impl;

public sealed class WindowFactory : IWindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WindowFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public LoginWindow CreateLoginWindow()
    {
        // Use root service provider directly for LoginWindow
        return _serviceProvider.GetRequiredService<LoginWindow>();
    }

    public MainWindow CreateMainWindow()
    {
        // Use root service provider directly for MainWindow
        return _serviceProvider.GetRequiredService<MainWindow>();
    }
}
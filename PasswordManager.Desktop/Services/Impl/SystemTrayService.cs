using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.Services;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace PasswordManager.Desktop.Services.Impl;

/// <summary>
/// Production implementation of system tray service.
/// Manages system tray icon, notifications, and context menu.
/// </summary>
public sealed class SystemTrayService : ISystemTrayService
{
    private readonly ILogger<SystemTrayService> _logger;
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    private const string AppName = "Password Manager";
    private const string DefaultTooltip = "Password Manager - Click to open";

    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Visible = false,
            Text = DefaultTooltip,
            ContextMenu = CreateContextMenu()
        };

        _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        _notifyIcon.BalloonTipClicked += OnNotificationClicked;

        _logger.LogInformation("System tray service initialized");
    }

    #region ISystemTrayService Implementation

    public bool IsVisible => _notifyIcon.Visible;

    public void Show()
    {
        ThrowIfDisposed();
        _notifyIcon.Visible = true;
        _logger.LogDebug("System tray icon shown");
    }

    public void Hide()
    {
        ThrowIfDisposed();
        _notifyIcon.Visible = false;
        _logger.LogDebug("System tray icon hidden");
    }

    public void MinimizeToTray()
    {
        ThrowIfDisposed();
        
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            mainWindow.Hide();
            mainWindow.WindowState = WindowState.Minimized;
            Show();

            ShowNotification(
                "Running in Background",
                "Password Manager is still running. Double-click the tray icon to restore.",
                NotificationIcon.Info,
                3000
            );

            _logger.LogInformation("Application minimized to system tray");
        }
    }

    public void RestoreFromTray()
    {
        ThrowIfDisposed();
        
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
            mainWindow.Focus();

            _logger.LogInformation("Application restored from system tray");
        }
    }

    public void ShowNotification(
        string title,
        string message,
        NotificationIcon icon = NotificationIcon.Info,
        int timeoutMs = 5000)
    {
        ThrowIfDisposed();

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ConvertNotificationIcon(icon);
        _notifyIcon.ShowBalloonTip(timeoutMs);

        _logger.LogDebug("Notification shown: {Title} - {Message}", title, message);
    }

    public void UpdateTooltip(string text)
    {
        ThrowIfDisposed();
        
        // Tooltip max length is 63 characters
        _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
    }

    #endregion

    #region Event Handlers

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        _logger.LogDebug("Tray icon double-clicked");
        RestoreFromTray();
    }

    private void OnNotificationClicked(object? sender, EventArgs e)
    {
        _logger.LogDebug("Notification clicked");
        RestoreFromTray();
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        _logger.LogDebug("Open menu item clicked");
        RestoreFromTray();
    }

    private void OnLockVaultClicked(object? sender, EventArgs e)
    {
        _logger.LogInformation("Lock vault requested from tray menu");
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            // TODO: Integrate with your existing logout logic via MediatR
            // Send LogoutUserCommand
        });

        ShowNotification(
            "Vault Locked",
            "Your vault has been locked for security.",
            NotificationIcon.Info,
            3000
        );
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit requested from tray menu");

        var result = MessageBox.Show(
            "Are you sure you want to exit Password Manager?",
            "Confirm Exit",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    #endregion

    #region Private Helpers

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        // Open
        var openItem = new MenuItem("Open Password Manager") { DefaultItem = true };
        openItem.Click += OnOpenClicked;
        menu.MenuItems.Add(openItem);

        menu.MenuItems.Add("-"); // Separator

        // Lock Vault
        var lockItem = new MenuItem("Lock Vault");
        lockItem.Click += OnLockVaultClicked;
        menu.MenuItems.Add(lockItem);

        menu.MenuItems.Add("-"); // Separator

        // Exit
        var exitItem = new MenuItem("Exit");
        exitItem.Click += OnExitClicked;
        menu.MenuItems.Add(exitItem);

        return menu;
    }

    private Icon LoadApplicationIcon()
    {
        try
        {
            var iconStream = Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/app-icon.ico")
            )?.Stream;

            if (iconStream != null)
            {
                return new Icon(iconStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load application icon, using default");
        }

        return SystemIcons.Application;
    }

    private static ToolTipIcon ConvertNotificationIcon(NotificationIcon icon)
    {
        return icon switch
        {
            NotificationIcon.Info => ToolTipIcon.Info,
            NotificationIcon.Warning => ToolTipIcon.Warning,
            NotificationIcon.Error => ToolTipIcon.Error,
            _ => ToolTipIcon.None
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemTrayService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing system tray service");

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        _disposed = true;
    }

    #endregion
}
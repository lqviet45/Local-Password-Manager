namespace PasswordManager.Desktop.Services;

/// <summary>
/// Interface for system tray operations (Desktop-only).
/// This service is NOT needed by the API backend.
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Shows the system tray icon.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the system tray icon.
    /// </summary>
    void Hide();

    /// <summary>
    /// Minimizes the main window to system tray.
    /// </summary>
    void MinimizeToTray();

    /// <summary>
    /// Restores the main window from system tray.
    /// </summary>
    void RestoreFromTray();

    /// <summary>
    /// Shows a balloon notification in the system tray.
    /// </summary>
    void ShowNotification(string title, string message, NotificationIcon icon = NotificationIcon.Info, int timeoutMs = 5000);

    /// <summary>
    /// Updates the tray icon tooltip text.
    /// </summary>
    void UpdateTooltip(string text);

    /// <summary>
    /// Checks if the tray icon is currently visible.
    /// </summary>
    bool IsVisible { get; }
}

/// <summary>
/// Notification icon types.
/// </summary>
public enum NotificationIcon
{
    None = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}
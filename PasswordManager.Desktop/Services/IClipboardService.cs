namespace PasswordManager.Desktop.Services;

/// <summary>
/// Service for secure clipboard operations with auto-clear functionality.
/// Desktop-only service - NOT needed by API backend.
/// </summary>
public interface IClipboardService : IDisposable
{
    /// <summary>
    /// Copies text to clipboard with auto-clear timer.
    /// </summary>
    /// <param name="text">Text to copy</param>
    /// <param name="clearAfter">Optional time before auto-clear (default: 30 seconds for sensitive data)</param>
    void CopyToClipboard(string text, TimeSpan? clearAfter = null);

    /// <summary>
    /// Copies password to clipboard with default 30-second auto-clear.
    /// </summary>
    void CopyPassword(string password);

    /// <summary>
    /// Copies username to clipboard (no auto-clear since it's less sensitive).
    /// </summary>
    void CopyUsername(string username);

    /// <summary>
    /// Copies credit card number to clipboard with 15-second auto-clear.
    /// </summary>
    void CopyCreditCardNumber(string cardNumber);

    /// <summary>
    /// Copies CVV to clipboard with 10-second auto-clear.
    /// </summary>
    void CopyCvv(string cvv);

    /// <summary>
    /// Immediately clears clipboard.
    /// </summary>
    void ClearClipboard();

    /// <summary>
    /// Cancels the auto-clear timer without clearing the clipboard.
    /// </summary>
    void CancelAutoClear();

    /// <summary>
    /// Gets the remaining time before auto-clear.
    /// Returns null if no auto-clear is active.
    /// </summary>
    TimeSpan? GetRemainingTime();

    /// <summary>
    /// Checks if auto-clear is currently active.
    /// </summary>
    bool IsAutoClearActive { get; }
}
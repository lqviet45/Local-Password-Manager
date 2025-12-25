namespace PasswordManager.Desktop.Services;

/// <summary>
/// Interface for global hotkey management (Desktop-only).
/// This service is NOT needed by the API backend.
/// </summary>
public interface IGlobalHotKeyService : IDisposable
{
    /// <summary>
    /// Initializes the hotkey service with a window handle.
    /// Must be called before registering any hotkeys.
    /// </summary>
    void Initialize(IntPtr windowHandle);

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="id">Unique identifier for this hotkey</param>
    /// <param name="modifiers">Modifier keys (Ctrl, Shift, Alt, Win)</param>
    /// <param name="virtualKeyCode">Virtual key code</param>
    /// <param name="callback">Action to execute when hotkey is pressed</param>
    /// <param name="description">Human-readable description</param>
    /// <returns>True if registered successfully, false otherwise</returns>
    bool RegisterHotKey(string id, HotKeyModifiers modifiers, int virtualKeyCode, Action callback, string description = "");

    /// <summary>
    /// Unregisters a specific hotkey by ID.
    /// </summary>
    bool UnregisterHotKey(string id);

    /// <summary>
    /// Unregisters all hotkeys.
    /// </summary>
    void UnregisterAllHotKeys();

    /// <summary>
    /// Gets information about all registered hotkeys.
    /// </summary>
    IReadOnlyList<HotKeyInfo> GetRegisteredHotKeys();

    /// <summary>
    /// Checks if the service is initialized.
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Modifier keys for hotkey combinations.
/// </summary>
[Flags]
public enum HotKeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Information about a registered hotkey.
/// </summary>
public sealed record HotKeyInfo(
    string Id,
    HotKeyModifiers Modifiers,
    int VirtualKeyCode,
    string Description,
    string DisplayText);
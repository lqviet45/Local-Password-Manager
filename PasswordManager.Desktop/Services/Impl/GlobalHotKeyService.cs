using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.Services;

namespace PasswordManager.Desktop.Services.Impl;

/// <summary>
/// Production implementation of global hotkey service.
/// Uses Windows API to register system-wide keyboard shortcuts.
/// </summary>
public sealed class GlobalHotKeyService : IGlobalHotKeyService
{
    private readonly ILogger<GlobalHotKeyService> _logger;
    private readonly Dictionary<string, RegisteredHotKey> _hotKeys;
    private HwndSource? _hwndSource;
    private bool _disposed;

    private const int WM_HOTKEY = 0x0312;

    #region Windows API

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    public GlobalHotKeyService(ILogger<GlobalHotKeyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hotKeys = new Dictionary<string, RegisteredHotKey>();

        _logger.LogInformation("Global hotkey service initialized");
    }

    #region IGlobalHotKeyService Implementation

    public bool IsInitialized => _hwndSource != null;

    public void Initialize(IntPtr windowHandle)
    {
        ThrowIfDisposed();

        if (IsInitialized)
        {
            _logger.LogWarning("Hotkey service already initialized");
            return;
        }

        _hwndSource = HwndSource.FromHwnd(windowHandle);

        if (_hwndSource != null)
        {
            _hwndSource.AddHook(HwndHook);
            _logger.LogInformation("Hotkey service attached to window handle");
        }
        else
        {
            _logger.LogError("Failed to get HwndSource from window handle");
            throw new InvalidOperationException("Failed to initialize hotkey service");
        }
    }

    public bool RegisterHotKey(
        string id,
        HotKeyModifiers modifiers,
        int virtualKeyCode,
        Action callback,
        string description = "")
    {
        ThrowIfDisposed();

        if (!IsInitialized)
        {
            _logger.LogError("Cannot register hotkey: Service not initialized. Call Initialize() first.");
            return false;
        }

        if (_hotKeys.ContainsKey(id))
        {
            _logger.LogWarning("Hotkey with ID '{Id}' already registered", id);
            return false;
        }

        var numericId = GetNextNumericId();

        try
        {
            var registered = RegisterHotKey(
                _hwndSource!.Handle,
                numericId,
                (uint)modifiers,
                (uint)virtualKeyCode
            );

            if (registered)
            {
                var hotKey = new RegisteredHotKey(
                    id,
                    numericId,
                    modifiers,
                    virtualKeyCode,
                    callback,
                    description
                );

                _hotKeys[id] = hotKey;

                _logger.LogInformation(
                    "Registered global hotkey: {DisplayText} ({Description})",
                    hotKey.DisplayText,
                    description
                );

                return true;
            }
            else
            {
                _logger.LogWarning(
                    "Failed to register hotkey: {Modifiers}+Key{VK}. It may be already in use.",
                    modifiers,
                    virtualKeyCode
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering hotkey: {Id}", id);
            return false;
        }
    }

    public bool UnregisterHotKey(string id)
    {
        ThrowIfDisposed();

        if (!IsInitialized || !_hotKeys.TryGetValue(id, out var hotKey))
            return false;

        try
        {
            var success = UnregisterHotKey(_hwndSource!.Handle, hotKey.NumericId);
            if (success)
            {
                _hotKeys.Remove(id);
                _logger.LogInformation("Unregistered hotkey: {Id}", id);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering hotkey: {Id}", id);
            return false;
        }
    }

    public void UnregisterAllHotKeys()
    {
        ThrowIfDisposed();

        if (!IsInitialized)
            return;

        var ids = _hotKeys.Keys.ToList();
        foreach (var id in ids)
        {
            UnregisterHotKey(id);
        }

        _logger.LogInformation("All hotkeys unregistered");
    }

    public IReadOnlyList<HotKeyInfo> GetRegisteredHotKeys()
    {
        return _hotKeys.Values
            .Select(hk => new HotKeyInfo(
                hk.Id,
                hk.Modifiers,
                hk.VirtualKeyCode,
                hk.Description,
                hk.DisplayText
            ))
            .ToList();
    }

    #endregion

    #region Private Methods

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var numericId = wParam.ToInt32();
            var hotKey = _hotKeys.Values.FirstOrDefault(hk => hk.NumericId == numericId);

            if (hotKey != null)
            {
                _logger.LogDebug("Hotkey triggered: {DisplayText}", hotKey.DisplayText);

                try
                {
                    hotKey.Callback?.Invoke();
                    handled = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing hotkey callback: {Id}", hotKey.Id);
                }
            }
        }

        return IntPtr.Zero;
    }

    private int GetNextNumericId()
    {
        // Find the next available numeric ID
        var usedIds = _hotKeys.Values.Select(hk => hk.NumericId).ToHashSet();
        for (int i = 1; i < 10000; i++)
        {
            if (!usedIds.Contains(i))
                return i;
        }
        throw new InvalidOperationException("No available hotkey IDs");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GlobalHotKeyService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing global hotkey service");

        UnregisterAllHotKeys();

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource = null;
        }

        _disposed = true;
    }

    #endregion

    #region Nested Types

    private sealed record RegisteredHotKey(
        string Id,
        int NumericId,
        HotKeyModifiers Modifiers,
        int VirtualKeyCode,
        Action Callback,
        string Description)
    {
        public string DisplayText => BuildDisplayText(Modifiers, VirtualKeyCode);

        private static string BuildDisplayText(HotKeyModifiers modifiers, int vk)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(HotKeyModifiers.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(HotKeyModifiers.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(HotKeyModifiers.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(HotKeyModifiers.Windows))
                parts.Add("Win");

            // Map common virtual key codes to readable names
            var keyName = vk switch
            {
                0x4C => "L",
                0x43 => "C",
                0x56 => "V",
                0x41 => "A",
                _ => $"VK{vk}"
            };

            parts.Add(keyName);

            return string.Join("+", parts);
        }
    }

    #endregion
}
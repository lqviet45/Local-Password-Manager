using PasswordManager.Shared.Users.Dto;

namespace PasswordManager.Desktop.Services.Impl;

public sealed class SessionService : ISessionService
{
    private UserDto? _currentUser;
    private DateTime _lastActivity;
    private readonly object _lock = new();

    public UserDto? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser;
            }
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            lock (_lock)
            {
                return _currentUser != null;
            }
        }
    }

    public event EventHandler? SessionEnding;
    public event EventHandler? SessionEnded;

    public void StartSession(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_lock)
        {
            _currentUser = user;
            _lastActivity = DateTime.UtcNow;
        }
    }

    public void EndSession()
    {
        SessionEnding?.Invoke(this, EventArgs.Empty);

        lock (_lock)
        {
            _currentUser = null;
            _lastActivity = DateTime.MinValue;
        }

        SessionEnded?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateLastActivity()
    {
        lock (_lock)
        {
            _lastActivity = DateTime.UtcNow;
        }
    }

    public bool IsSessionTimedOut(TimeSpan timeout)
    {
        lock (_lock)
        {
            if (_currentUser == null)
            {
                return true;
            }

            var elapsed = DateTime.UtcNow - _lastActivity;
            return elapsed > timeout;
        }
    }
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MediatR;
using PasswordManager.Desktop.Services;
using PasswordManager.Domain.Enums;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Domain.ValueObjects;
using PasswordManager.Shared.Common.Result;
using PasswordManager.Shared.Vault.Commands;
using PasswordManager.Shared.Vault.Dto;

namespace PasswordManager.Desktop.ViewModels;

/// <summary>
/// ViewModel for adding or editing vault items.
/// Handles encryption, password generation, and strength checking.
/// </summary>
public partial class AddEditItemViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ISessionService _sessionService;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IMasterPasswordService _masterPasswordService;
    private readonly IPasswordStrengthService _passwordStrengthService;
    private readonly IHibpService _hibpService;

    private VaultItemDto? _existingItem;
    private Action<VaultItemDto>? _onSaveCallback;

    [ObservableProperty]
    private VaultItemType _selectedType = VaultItemType.Login;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private string _passwordStrengthText = string.Empty;

    [ObservableProperty]
    private string _passwordStrengthColor = "Gray";

    [ObservableProperty]
    private int _passwordStrengthScore;

    [ObservableProperty]
    private bool _isPasswordBreached;

    [ObservableProperty]
    private int _breachCount;

    [ObservableProperty]
    private bool _isCheckingBreach;

    [ObservableProperty]
    private ObservableCollection<VaultItemType> _availableTypes = new();

    // Property to signal successful save
    [ObservableProperty]
    private bool _shouldCloseWindow;

    public bool IsEditMode => _existingItem != null;
    public string WindowTitle => IsEditMode ? $"Edit {_existingItem!.Name}" : "Add New Item";

    public AddEditItemViewModel(
        IMediator mediator,
        ISessionService sessionService,
        ICryptoProvider cryptoProvider,
        IMasterPasswordService masterPasswordService,
        IPasswordStrengthService passwordStrengthService,
        IHibpService hibpService,
        IDialogService dialogService,
        ILogger<AddEditItemViewModel> logger)
        : base(dialogService, logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _masterPasswordService = masterPasswordService ?? throw new ArgumentNullException(nameof(masterPasswordService));
        _passwordStrengthService = passwordStrengthService ?? throw new ArgumentNullException(nameof(passwordStrengthService));
        _hibpService = hibpService ?? throw new ArgumentNullException(nameof(hibpService));

        Title = "Add Item";

        // Load available types
        AvailableTypes = new ObservableCollection<VaultItemType>(
            Enum.GetValues<VaultItemType>());
    }

    /// <summary>
    /// Initialize for creating new item
    /// </summary>
    public void InitializeForCreate(Action<VaultItemDto> onSave)
    {
        _onSaveCallback = onSave;
        _existingItem = null;
        Title = "Add New Item";
    }

    /// <summary>
    /// Initialize for editing existing item
    /// </summary>
    public async Task InitializeForEditAsync(VaultItemDto item, Action<VaultItemDto> onSave)
    {
        _existingItem = item ?? throw new ArgumentNullException(nameof(item));
        _onSaveCallback = onSave;
        Title = $"Edit {item.Name}";

        await ExecuteAsync(async () =>
        {
            // Decrypt existing password
            var encryptionKey = _masterPasswordService.GetEncryptionKey();
            var encryptedData = EncryptedData.FromCombinedString(item.EncryptedData);
            var decryptedPassword = await _cryptoProvider.DecryptAsync(encryptedData, encryptionKey);

            // Load item data
            SelectedType = item.Type;
            Name = item.Name;
            Username = item.Username ?? string.Empty;
            Password = decryptedPassword;
            Url = item.Url ?? string.Empty;
            Notes = item.Notes ?? string.Empty;
            Tags = item.Tags ?? string.Empty;
            IsFavorite = item.IsFavorite;

            Logger.LogInformation("Loaded item for editing: {ItemId}", item.Id);
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateInput())
            return;

        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("Saving vault item: {Name}", Name);

            // Get encryption key
            var encryptionKey = _masterPasswordService.GetEncryptionKey();

            var request = new VaultItemRequest
            {
                Type = SelectedType,
                Name = Name.Trim(),
                Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                Password = Password,
                Url = string.IsNullOrWhiteSpace(Url) ? null : Url.Trim(),
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags.Trim(),
                IsFavorite = IsFavorite
            };

            Result<VaultItemDto> result;
            if (_existingItem == null)
            {
                result = await _mediator.Send(new CreateVaultItemCommand(_sessionService.CurrentUser!.Id, request, encryptionKey));
                Logger.LogInformation("Created new vault item");
            }
            else
            {
                result = await _mediator.Send(new UpdateVaultItemCommand(_sessionService.CurrentUser!.Id, _existingItem.Id, request, encryptionKey));
                Logger.LogInformation("Updated vault item: {ItemId}", _existingItem.Id);
            }

            if (result.IsFailure || result.Value == null)
            {
                throw new InvalidOperationException(result.Error?.Message ?? "Failed to save item");
            }

            // Callback to parent ViewModel
            _onSaveCallback?.Invoke(result.Value);

            Logger.LogInformation("Save successful, signaling window to close");
            
            // Set flag to trigger window close
            ShouldCloseWindow = true;

        }, "Failed to save item");
    }

    [RelayCommand]
    private async Task GeneratePasswordAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Generate strong random password
            var length = 20; // Default length
            var includeSymbols = true;
            var includeNumbers = true;
            var includeUppercase = true;
            var includeLowercase = true;

            var password = GenerateSecurePassword(
                length, 
                includeLowercase, 
                includeUppercase, 
                includeNumbers, 
                includeSymbols);

            Password = password;

            Logger.LogInformation("Generated password with length {Length}", length);
            ShowInfo("Strong password generated!");

            await Task.CompletedTask;
        });
    }

    [RelayCommand]
    private async Task CheckPasswordBreachAsync()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowWarning("Please enter a password to check");
            return;
        }

        IsCheckingBreach = true;
        
        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("Checking password breach status...");

            var result = await _hibpService.CheckPasswordAsync(Password);

            IsPasswordBreached = result.IsBreached;
            BreachCount = result.BreachCount;

            if (result.IsBreached)
            {
                ShowWarning(
                    $"⚠️ This password has been found in {result.BreachCount:N0} data breaches!\n\n" +
                    "It is strongly recommended to use a different password.",
                    "Security Warning");
            }
            else
            {
                ShowInfo("✓ This password has not been found in known data breaches.", "Good News");
            }

            Logger.LogInformation("Breach check complete. Breached: {IsBreached}, Count: {Count}", 
                result.IsBreached, result.BreachCount);

        }, "Failed to check password breach status");

        IsCheckingBreach = false;
    }

    [RelayCommand]
    private void ToggleShowPassword()
    {
        ShowPassword = !ShowPassword;
    }

    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            UpdatePasswordStrength(value);
        }
        else
        {
            PasswordStrengthText = string.Empty;
            PasswordStrengthScore = 0;
            IsPasswordBreached = false;
            BreachCount = 0;
        }
    }

    private void UpdatePasswordStrength(string password)
    {
        var analysis = _passwordStrengthService.AnalyzePassword(password);
        
        PasswordStrengthScore = analysis.Score;
        PasswordStrengthText = analysis.Level switch
        {
            StrengthLevel.VeryWeak => "Very Weak",
            StrengthLevel.Weak => "Weak",
            StrengthLevel.Fair => "Fair",
            StrengthLevel.Strong => "Strong",
            StrengthLevel.VeryStrong => "Very Strong",
            _ => "Unknown"
        };

        PasswordStrengthColor = analysis.Level switch
        {
            StrengthLevel.VeryWeak => "#D32F2F",
            StrengthLevel.Weak => "#F57C00",
            StrengthLevel.Fair => "#FBC02D",
            StrengthLevel.Strong => "#689F38",
            StrengthLevel.VeryStrong => "#388E3C",
            _ => "Gray"
        };
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ShowError("Please enter a name for this item");
            return false;
        }

        if (SelectedType == VaultItemType.Login && string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Please enter a password");
            return false;
        }

        if (SelectedType == VaultItemType.Login && 
            _passwordStrengthService.EvaluateStrength(Password) < StrengthLevel.Fair)
        {
            if (!Confirm(
                "This password is weak. Are you sure you want to save it?",
                "Weak Password"))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    private string GenerateSecurePassword(
        int length,
        bool includeLowercase,
        bool includeUppercase,
        bool includeNumbers,
        bool includeSymbols)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var characterSet = string.Empty;
        if (includeLowercase) characterSet += lowercase;
        if (includeUppercase) characterSet += uppercase;
        if (includeNumbers) characterSet += numbers;
        if (includeSymbols) characterSet += symbols;

        if (string.IsNullOrEmpty(characterSet))
        {
            characterSet = lowercase + uppercase + numbers + symbols;
        }

        var password = new char[length];
        var randomBytes = _cryptoProvider.GenerateRandomKey(length);

        for (int i = 0; i < length; i++)
        {
            password[i] = characterSet[randomBytes[i] % characterSet.Length];
        }

        // Ensure at least one character from each selected category
        var random = new Random();
        if (includeLowercase && !password.Any(c => lowercase.Contains(c)))
        {
            password[random.Next(length)] = lowercase[randomBytes[0] % lowercase.Length];
        }
        if (includeUppercase && !password.Any(c => uppercase.Contains(c)))
        {
            password[random.Next(length)] = uppercase[randomBytes[1] % uppercase.Length];
        }
        if (includeNumbers && !password.Any(c => numbers.Contains(c)))
        {
            password[random.Next(length)] = numbers[randomBytes[2] % numbers.Length];
        }
        if (includeSymbols && !password.Any(c => symbols.Contains(c)))
        {
            password[random.Next(length)] = symbols[randomBytes[3] % symbols.Length];
        }

        return new string(password);
    }
}
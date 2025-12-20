using System.Windows;
using Microsoft.Extensions.Logging;
using PasswordManager.Desktop.ViewModels;

namespace PasswordManager.Desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ILogger<MainWindow>? _logger;

    public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;
        
        _logger?.LogInformation("=== MainWindow Constructor ===");
        _logger?.LogInformation("MainViewModel Type: {Type}", _viewModel.GetType().Name);
        _logger?.LogInformation("CurrentViewModel: {Type}", _viewModel.CurrentViewModel?.GetType().Name ?? "NULL");
        
        InitializeComponent();
        
        // Set DataContext BEFORE any bindings
        DataContext = _viewModel;
        
        _logger?.LogInformation("DataContext set to MainViewModel");
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("=== MainWindow Loaded ===");
        _logger?.LogInformation("DataContext Type: {Type}", DataContext?.GetType().Name ?? "NULL");
        
        if (DataContext is MainViewModel vm)
        {
            _logger?.LogInformation("✓ DataContext is MainViewModel");
            _logger?.LogInformation("MainViewModel.CurrentViewModel: {Type}", 
                vm.CurrentViewModel?.GetType().Name ?? "NULL");
            
            // Force set CurrentViewModel if null
            if (vm.CurrentViewModel == null)
            {
                _logger?.LogWarning("CurrentViewModel is NULL! Setting to VaultViewModel");
                vm.CurrentViewModel = vm.VaultViewModel;
            }
            
            // Load vault items when window loads
            if (vm.VaultViewModel != null)
            {
                _logger?.LogInformation("Loading VaultViewModel items...");
                try
                {
                    await vm.VaultViewModel.LoadItemsAsync();
                    _logger?.LogInformation("VaultViewModel loaded successfully. Items count: {Count}", 
                        vm.VaultViewModel.TotalItemsCount);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load vault items");
                }
            }
            else
            {
                _logger?.LogError("VaultViewModel is NULL!");
            }
        }
        else
        {
            _logger?.LogError("❌ DataContext is NOT MainViewModel! Type: {Type}", 
                DataContext?.GetType().Name ?? "NULL");
        }
    }
}
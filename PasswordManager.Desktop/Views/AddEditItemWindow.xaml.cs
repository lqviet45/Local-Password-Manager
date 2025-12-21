using System.Windows;
using PasswordManager.Desktop.ViewModels;

namespace PasswordManager.Desktop.Views;

/// <summary>
/// Interaction logic for AddEditItemWindow.xaml
/// FIXED: Properly monitors ShouldCloseWindow property and closes window on success
/// </summary>
public partial class AddEditItemWindow : Window
{
    private readonly AddEditItemViewModel _viewModel;

    public AddEditItemWindow(AddEditItemViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        
        // Subscribe to property changes to detect successful save
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Monitor ShouldCloseWindow flag
        if (e.PropertyName == nameof(AddEditItemViewModel.ShouldCloseWindow))
        {
            if (_viewModel.ShouldCloseWindow)
            {
                // Success - close window with positive result
                DialogResult = true;
                Close();
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        base.OnClosed(e);
    }
}
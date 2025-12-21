using System.Windows;
using PasswordManager.Desktop.ViewModels;

namespace PasswordManager.Desktop.Views;

/// <summary>
/// Interaction logic for AddEditItemWindow.xaml
/// </summary>
public partial class AddEditItemWindow : Window
{
    public AddEditItemWindow(AddEditItemViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        
        // Subscribe to save success event to close window
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ErrorMessage) && 
                string.IsNullOrEmpty(viewModel.ErrorMessage))
            {
                // Success - close window after a short delay
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(500);
                    DialogResult = true;
                    Close();
                });
            }
        };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
